using OneMoi.Application.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Api.Security;

/// <summary>
/// Runs on EVERY authenticated request (after the JWT is validated) and checks that the login is
/// still allowed right now — a valid-looking token is not enough:
///  • user / operator still exists and is active (not deactivated by owner or admin)
///  • security stamp unchanged (password change, "logout all devices", role change → old tokens die)
///  • vendor staff: membership + role still the same as when the token was issued
///  • vendor not suspended by OneMoi (operators also need the vendor to be Active)
/// Uses a 30-second cache (ISessionStateStore) so this is cheap.
/// </summary>
public class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, ICurrentUser current, ISessionStateStore sessions)
    {
        if (current.IsAuthenticated && current.PrincipalType is { } type)
        {
            var id = current.OperatorId ?? current.UserId;
            var state = id == null ? null : await sessions.GetPrincipalAsync(type, id.Value, ctx.RequestAborted);

            if (state == null || !state.IsActive)
            {
                await Reject(ctx, 401, "SESSION_REVOKED", "Your login has been disabled or removed. Please contact your administrator.");
                return;
            }
            if (state.SecurityStamp != current.SecurityStamp)
            {
                await Reject(ctx, 401, "SESSION_REVOKED", "You were signed out (password changed or logged out from all devices). Please login again.");
                return;
            }
            if (current.UserType == UserType.TenantUser && (state.TenantId != current.TenantId || state.TenantRole != current.TenantRole))
            {
                await Reject(ctx, 401, "ROLE_CHANGED", "Your access was changed by the vendor owner. Please login again.");
                return;
            }
            if (current.TenantId is { } tenantId)
            {
                var status = await sessions.GetTenantStatusAsync(tenantId, ctx.RequestAborted);
                if (status == TenantStatus.Suspended || (current.IsOperator && status != TenantStatus.Active))
                {
                    await Reject(ctx, 403, "TENANT_SUSPENDED", "This vendor account is suspended. Please contact OneMoi support.");
                    return;
                }
            }
        }
        await next(ctx);
    }

    private static Task Reject(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { message, code });
    }
}

/// <summary>Turns authorization failures into the same JSON shape as other errors, with a helpful message.</summary>
public class JsonAuthorizationResultHandler : Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler
{
    private readonly Microsoft.AspNetCore.Authorization.Policy.AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext ctx, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy,
        Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult result)
    {
        if (result.Challenged)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { message = "Please login to continue.", code = "UNAUTHENTICATED" });
            return;
        }
        if (result.Forbidden)
        {
            var reasons = result.AuthorizationFailure?.FailureReasons.Select(r => r.Message).ToList() ?? new();
            var pending = reasons.Contains("TENANT_NOT_ACTIVE");
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(pending
                ? new { message = "Your vendor account is waiting for OneMoi approval. Functions and Moi entry open after approval.", code = "TENANT_PENDING" }
                : new { message = "You do not have permission to do this.", code = "FORBIDDEN" });
            return;
        }
        await _default.HandleAsync(next, ctx, policy, result);
    }
}

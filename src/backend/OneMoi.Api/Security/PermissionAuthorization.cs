using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Enums;

namespace OneMoi.Api.Security;

/// <summary>
/// ROLE-BASED AUTHORIZATION — what you may do.
/// Usage on a controller or action:  [HasPermission(Permissions.FunctionsManage)]
/// The permission → role mapping lives in Application/Common/Security/Permissions.cs.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(PolicyPrefix + permission)
{
    public const string PolicyPrefix = "perm:";
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>Creates a policy on the fly for every "perm:…" name, so we never register 30 policies by hand.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[HasPermissionAttribute.PolicyPrefix.Length..]))
            .Build();
    }
}

/// <summary>
/// Grants the requirement when the caller's role has the permission.
/// TENANT AUTHORIZATION: money-changing permissions also need the vendor to be Active (not Pending).
/// </summary>
public sealed class PermissionHandler(ICurrentUser current, ISessionStateStore sessions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!current.Can(requirement.Permission))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Missing permission {requirement.Permission}"));
            return;
        }
        if (Permissions.RequireActiveTenant.Contains(requirement.Permission) && current.TenantId != null)
        {
            var status = await sessions.GetTenantStatusAsync(current.TenantId.Value);
            if (status != TenantStatus.Active)
            {
                context.Fail(new AuthorizationFailureReason(this, "TENANT_NOT_ACTIVE"));
                return;
            }
        }
        context.Succeed(requirement);
    }
}

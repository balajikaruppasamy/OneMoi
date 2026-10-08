using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;

namespace OneMoi.Api.Infrastructure;

/// <summary>
/// Turns exceptions into one JSON shape the Angular app understands:
/// { "message": "...", "errors": { "field": ["..."] } }
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log, IHostEnvironment env)
{
    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (AppException ex)
        {
            await Write(ctx, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                                           || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
        {
            log.LogWarning(ex, "Duplicate key");
            await Write(ctx, 409, "This record already exists (duplicate). Please refresh and try again.", null);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // client went away
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled error on {Path}", ctx.Request.Path);
            await Write(ctx, 500, env.IsDevelopment() ? ex.GetBaseException().Message : "Something went wrong. Please try again.", null);
        }
    }

    private static Task Write(HttpContext ctx, int status, string message, IDictionary<string, string[]>? errors)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new { message, errors });
    }
}

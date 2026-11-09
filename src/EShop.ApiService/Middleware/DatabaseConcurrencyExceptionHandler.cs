using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EShop.ApiService.Middleware;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Returns a safe conflict response when an optimistic update is stale.
internal sealed class DatabaseConcurrencyExceptionHandler(
    ILogger<DatabaseConcurrencyExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        logger.LogWarning(
            exception,
            "Database concurrency conflict for request {Path}",
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "The resource changed during this operation. Reload it and try again.",
            Instance = httpContext.Request.Path
        }, cancellationToken);
        return true;
    }
}

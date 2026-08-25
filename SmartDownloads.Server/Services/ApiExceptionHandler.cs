using Microsoft.AspNetCore.Diagnostics;
using SmartDownloads.Server.Models;
namespace SmartDownloads.Server.Services;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ServiceUnavailableException) { context.Response.StatusCode = 503; await context.Response.WriteAsJsonAsync(new ApiError("service_unavailable", exception.Message), cancellationToken); return true; }
        logger.LogError(exception, "Unhandled server error"); context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new ApiError("server_error", "An unexpected error occurred."), cancellationToken); return true;
    }
}

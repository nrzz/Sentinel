using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sentinel.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? exception.Message
                : "Please contact support with the correlation ID.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problem), cancellationToken);
        return true;
    }
}

using Sentinel.Domain.Common;

namespace Sentinel.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault()
            ?? CorrelationId.New();

        context.Items[CorrelationId.HeaderName] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}

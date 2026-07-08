namespace Sentinel.Api.Features.Traces;

public static class TraceExtensions
{
    public static IServiceCollection AddTraceFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTraceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestTracesEndpoint();
        app.MapQueryTracesEndpoints();
        return app;
    }
}

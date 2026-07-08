namespace Sentinel.Api.Features.Metrics;

public static class MetricExtensions
{
    public static IServiceCollection AddMetricFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapMetricEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestMetricsEndpoint();
        app.MapQueryMetricsEndpoint();
        return app;
    }
}

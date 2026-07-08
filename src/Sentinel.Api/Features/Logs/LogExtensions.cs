namespace Sentinel.Api.Features.Logs;

public static class LogExtensions
{
    public static IServiceCollection AddLogFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapLogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIngestLogsEndpoint();
        return app;
    }
}

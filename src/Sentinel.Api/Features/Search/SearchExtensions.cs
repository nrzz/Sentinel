namespace Sentinel.Api.Features.Search;

public static class SearchExtensions
{
    public static IServiceCollection AddSearchFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSearchLogsEndpoint();
        app.MapSavedSearchEndpoints();
        return app;
    }
}

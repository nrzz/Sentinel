namespace Sentinel.Api.Features.Deployments;

public static class DeploymentExtensions
{
    public static IServiceCollection AddDeploymentFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapDeploymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDeploymentWebhookEndpoint();
        return app;
    }
}

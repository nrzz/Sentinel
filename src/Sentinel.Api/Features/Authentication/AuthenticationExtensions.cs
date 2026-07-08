namespace Sentinel.Api.Features.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationFeatures(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        return services;
    }

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/login", LoginEndpoint.Handle)
            .WithName("Login")
            .AllowAnonymous()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", RegisterEndpoint.Handle)
            .WithName("Register")
            .AllowAnonymous()
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/refresh", RefreshEndpoint.Handle)
            .WithName("RefreshToken")
            .AllowAnonymous()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}

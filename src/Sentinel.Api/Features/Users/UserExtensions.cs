namespace Sentinel.Api.Features.Users;

public static class UserExtensions
{
    public static IServiceCollection AddUserFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/", ListUsersEndpoint.Handle)
            .WithName("ListUsers")
            .Produces<IReadOnlyList<UserResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetUserEndpoint.Handle)
            .WithName("GetUser")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUserEndpoint.Handle)
            .WithName("CreateUser")
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateUserEndpoint.Handle)
            .WithName("UpdateUser")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUserEndpoint.Handle)
            .WithName("DeleteUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

using Sentinel.Api.Authorization;

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
            .RequireAuthorization(SentinelPolicies.UsersRead)
            .Produces<IReadOnlyList<UserResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetUserEndpoint.Handle)
            .WithName("GetUser")
            .RequireAuthorization(SentinelPolicies.UsersRead)
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUserEndpoint.Handle)
            .WithName("CreateUser")
            .RequireAuthorization(SentinelPolicies.UsersWrite)
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateUserEndpoint.Handle)
            .WithName("UpdateUser")
            .RequireAuthorization(SentinelPolicies.UsersWrite)
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUserEndpoint.Handle)
            .WithName("DeleteUser")
            .RequireAuthorization(SentinelPolicies.UsersDelete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

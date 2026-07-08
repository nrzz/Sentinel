using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Users;

internal static class CreateUserEndpoint
{
    public static async Task<IResult> Handle(
        CreateUserApiRequest request,
        IUserService userService,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
        {
            return ApiResults.BadRequest("X-Tenant-ID header is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ApiResults.BadRequest("Email, password, and display name are required.");
        }

        var user = await userService.CreateAsync(
            tenantContext.RequireTenantId(),
            new CreateUserRequest(
                request.Email,
                request.Password,
                request.DisplayName,
                request.RoleId),
            cancellationToken);

        if (user is null)
        {
            return ApiResults.Conflict("User already exists or role is invalid.");
        }

        return Results.Created($"/api/v1/users/{user.Id}", ListUsersEndpoint.MapUser(user));
    }
}

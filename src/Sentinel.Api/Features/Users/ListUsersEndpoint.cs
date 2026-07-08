using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Users;

internal static class ListUsersEndpoint
{
    public static async Task<IResult> Handle(
        IUserService userService,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
        {
            return ApiResults.BadRequest("X-Tenant-ID header is required.");
        }

        var users = await userService.ListAsync(tenantContext.RequireTenantId(), cancellationToken);
        return Results.Ok(users.Select(MapUser));
    }

    internal static UserResponse MapUser(UserDto user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.EmailVerified,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt);
}

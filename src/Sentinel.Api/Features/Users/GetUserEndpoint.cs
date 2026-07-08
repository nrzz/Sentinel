using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Users;

internal static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        IUserService userService,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
        {
            return ApiResults.BadRequest("X-Tenant-ID header is required.");
        }

        var user = await userService.GetByIdAsync(tenantContext.RequireTenantId(), id, cancellationToken);
        if (user is null)
        {
            return ApiResults.NotFound($"User '{id}' was not found in the current tenant.");
        }

        return Results.Ok(ListUsersEndpoint.MapUser(user));
    }
}

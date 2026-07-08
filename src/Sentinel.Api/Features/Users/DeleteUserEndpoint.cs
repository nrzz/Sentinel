using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Users;

internal static class DeleteUserEndpoint
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

        var deleted = await userService.DeleteAsync(tenantContext.RequireTenantId(), id, cancellationToken);
        if (!deleted)
        {
            return ApiResults.NotFound($"User '{id}' was not found in the current tenant.");
        }

        return Results.NoContent();
    }
}

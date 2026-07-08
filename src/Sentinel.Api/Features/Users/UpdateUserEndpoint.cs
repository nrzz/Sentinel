using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Users;

internal static class UpdateUserEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        UpdateUserApiRequest request,
        IUserService userService,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
        {
            return ApiResults.BadRequest("X-Tenant-ID header is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ApiResults.BadRequest("Display name is required.");
        }

        var user = await userService.UpdateAsync(
            tenantContext.RequireTenantId(),
            id,
            new UpdateUserRequest(request.DisplayName, request.IsActive, request.Password),
            cancellationToken);

        if (user is null)
        {
            return ApiResults.NotFound($"User '{id}' was not found in the current tenant.");
        }

        return Results.Ok(ListUsersEndpoint.MapUser(user));
    }
}

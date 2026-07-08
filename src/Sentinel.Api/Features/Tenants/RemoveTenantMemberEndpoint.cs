using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class RemoveTenantMemberEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        Guid userId,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var removed = await tenantService.RemoveMemberAsync(id, userId, cancellationToken);
        if (!removed)
        {
            return ApiResults.NotFound($"Member '{userId}' was not found in tenant '{id}'.");
        }

        return Results.NoContent();
    }
}

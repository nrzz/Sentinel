using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class DeleteTenantEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var deleted = await tenantService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return ApiResults.NotFound($"Tenant '{id}' was not found.");
        }

        return Results.NoContent();
    }
}

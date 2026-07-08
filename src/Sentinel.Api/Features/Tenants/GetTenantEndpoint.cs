using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class GetTenantEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return ApiResults.NotFound($"Tenant '{id}' was not found.");
        }

        return Results.Ok(ListTenantsEndpoint.MapTenant(tenant));
    }
}

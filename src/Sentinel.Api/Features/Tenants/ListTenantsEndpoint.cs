using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class ListTenantsEndpoint
{
    public static async Task<IResult> Handle(
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantService.ListAsync(cancellationToken);
        return Results.Ok(tenants.Select(MapTenant));
    }

    internal static TenantResponse MapTenant(TenantDto tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.Environment,
            tenant.SettingsJson,
            tenant.CreatedAt,
            tenant.UpdatedAt);
}

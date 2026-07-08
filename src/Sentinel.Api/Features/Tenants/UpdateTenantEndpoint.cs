using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class UpdateTenantEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        UpdateTenantApiRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return ApiResults.BadRequest("Name and slug are required.");
        }

        var tenant = await tenantService.UpdateAsync(
            id,
            new UpdateTenantRequest(
                request.Name,
                request.Slug,
                request.IsActive,
                request.Environment,
                request.SettingsJson),
            cancellationToken);

        if (tenant is null)
        {
            return ApiResults.NotFound($"Tenant '{id}' was not found or slug is already in use.");
        }

        return Results.Ok(ListTenantsEndpoint.MapTenant(tenant));
    }
}

using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class CreateTenantEndpoint
{
    public static async Task<IResult> Handle(
        CreateTenantApiRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return ApiResults.BadRequest("Name and slug are required.");
        }

        var tenant = await tenantService.CreateAsync(
            new CreateTenantRequest(request.Name, request.Slug, request.Environment),
            cancellationToken);

        if (tenant is null)
        {
            return ApiResults.Conflict("A tenant with the provided slug already exists.");
        }

        return Results.Created($"/api/v1/tenants/{tenant.Id}", ListTenantsEndpoint.MapTenant(tenant));
    }
}

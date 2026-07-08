using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class AddTenantMemberEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        AddTenantMemberApiRequest request,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || request.RoleId == Guid.Empty)
        {
            return ApiResults.BadRequest("UserId and RoleId are required.");
        }

        var member = await tenantService.AddMemberAsync(
            id,
            new AddTenantMemberRequest(request.UserId, request.RoleId),
            cancellationToken);

        if (member is null)
        {
            return ApiResults.NotFound("Tenant, user, or role was not found.");
        }

        return Results.Created(
            $"/api/v1/tenants/{id}/members/{member.UserId}",
            ListTenantMembersEndpoint.MapMember(member));
    }
}

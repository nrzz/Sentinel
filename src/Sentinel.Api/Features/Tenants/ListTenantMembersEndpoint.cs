using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Tenants;

internal static class ListTenantMembersEndpoint
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

        var members = await tenantService.ListMembersAsync(id, cancellationToken);
        return Results.Ok(members.Select(MapMember));
    }

    internal static TenantMemberResponse MapMember(TenantMemberDto member) =>
        new(
            member.Id,
            member.TenantId,
            member.UserId,
            member.UserEmail,
            member.UserDisplayName,
            member.RoleId,
            member.RoleName,
            member.IsActive,
            member.JoinedAt);
}

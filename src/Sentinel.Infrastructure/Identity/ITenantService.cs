namespace Sentinel.Infrastructure.Identity;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    string Environment,
    string SettingsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantMemberDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    Guid RoleId,
    string RoleName,
    bool IsActive,
    DateTimeOffset JoinedAt);

public sealed record CreateTenantRequest(string Name, string Slug, string Environment = "production");
public sealed record UpdateTenantRequest(string Name, string Slug, bool IsActive, string Environment, string? SettingsJson);
public sealed record AddTenantMemberRequest(Guid UserId, Guid RoleId);

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantDto?> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto?> UpdateAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMemberDto>> ListMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantMemberDto?> AddMemberAsync(Guid tenantId, AddTenantMemberRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}

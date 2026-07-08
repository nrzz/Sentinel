namespace Sentinel.Api.Features.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    string Environment,
    string SettingsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TenantMemberResponse(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    Guid RoleId,
    string RoleName,
    bool IsActive,
    DateTimeOffset JoinedAt);

public sealed record CreateTenantApiRequest(string Name, string Slug, string Environment = "production");

public sealed record UpdateTenantApiRequest(
    string Name,
    string Slug,
    bool IsActive,
    string Environment,
    string? SettingsJson);

public sealed record AddTenantMemberApiRequest(Guid UserId, Guid RoleId);

using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class ApiKey : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string[] Scopes { get; private set; } = [];
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    private ApiKey()
    {
    }

    public static ApiKey Create(
        Guid tenantId,
        string name,
        string keyHash,
        string keyPrefix,
        IEnumerable<string> scopes,
        Guid? createdByUserId,
        DateTimeOffset? expiresAt = null)
    {
        return new ApiKey
        {
            TenantId = tenantId,
            Name = name.Trim(),
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = scopes.Select(s => s.Trim().ToLowerInvariant()).Distinct().ToArray(),
            CreatedByUserId = createdByUserId,
            ExpiresAt = expiresAt
        };
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Revoke()
    {
        IsActive = false;
        Touch();
    }

    public static ApiKey FromPersistence(
        Guid id,
        Guid tenantId,
        string name,
        string keyHash,
        string keyPrefix,
        string[] scopes,
        DateTimeOffset? expiresAt,
        bool isActive,
        Guid? createdByUserId,
        DateTimeOffset? lastUsedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = scopes,
            ExpiresAt = expiresAt,
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            LastUsedAt = lastUsedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}

using Dapper;
using Sentinel.Domain.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public ApiKeyRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ApiKeyRow>(
            """
            SELECT id, tenant_id, name, key_hash, key_prefix, scopes, expires_at, is_active,
                   created_by_user_id, last_used_at, created_at, updated_at
            FROM tenant.api_keys
            WHERE key_hash = @KeyHash AND is_active = true
            """,
            new { KeyHash = keyHash });

        if (row is null)
        {
            return null;
        }

        if (row.ExpiresAt.HasValue && row.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return null;
        }

        return ApiKey.FromPersistence(
            row.Id,
            row.TenantId,
            row.Name,
            row.KeyHash,
            row.KeyPrefix,
            row.Scopes ?? [],
            row.ExpiresAt,
            row.IsActive,
            row.CreatedByUserId,
            row.LastUsedAt,
            row.CreatedAt,
            row.UpdatedAt);
    }

    public async Task UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE tenant.api_keys
            SET last_used_at = @Now, updated_at = @Now
            WHERE id = @Id
            """,
            new { Id = id, Now = DateTimeOffset.UtcNow });
    }

    private sealed record ApiKeyRow(
        Guid Id,
        Guid TenantId,
        string Name,
        string KeyHash,
        string KeyPrefix,
        string[]? Scopes,
        DateTimeOffset? ExpiresAt,
        bool IsActive,
        Guid? CreatedByUserId,
        DateTimeOffset? LastUsedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

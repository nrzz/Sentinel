using Dapper;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class RbacService : IRbacService
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public RbacService(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var permissions = await connection.QueryAsync<string>(
            """
            SELECT DISTINCT p.name
            FROM identity.user_roles ur
            INNER JOIN identity.role_permissions rp ON rp.role_id = ur.role_id
            INNER JOIN identity.permissions p ON p.id = rp.permission_id
            WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId
            ORDER BY p.name
            """,
            new { UserId = userId, TenantId = tenantId });

        return permissions.ToList();
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var roles = await connection.QueryAsync<string>(
            """
            SELECT DISTINCT r.name
            FROM identity.user_roles ur
            INNER JOIN identity.roles r ON r.id = ur.role_id
            WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId
            ORDER BY r.name
            """,
            new { UserId = userId, TenantId = tenantId });

        return roles.ToList();
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task AssignRoleAsync(
        Guid userId,
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO identity.user_roles (user_id, role_id, tenant_id)
            VALUES (@UserId, @RoleId, @TenantId)
            ON CONFLICT (user_id, role_id, tenant_id) DO NOTHING
            """,
            new { UserId = userId, RoleId = roleId, TenantId = tenantId });
    }
}

using Dapper;
using Sentinel.Domain.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class TenantService : ITenantService
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;
    private readonly IRbacService _rbacService;
    private readonly IAuditService _auditService;

    public TenantService(
        IPostgreSqlConnectionFactory connectionFactory,
        IRbacService rbacService,
        IAuditService auditService)
    {
        _connectionFactory = connectionFactory;
        _rbacService = rbacService;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<TenantDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var tenants = await connection.QueryAsync<TenantDto>(
            """
            SELECT id AS Id, name AS Name, slug AS Slug, is_active AS IsActive, environment AS Environment,
                   settings::text AS SettingsJson, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tenant.tenants
            ORDER BY name
            """);

        return tenants.ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TenantDto>(
            """
            SELECT id AS Id, name AS Name, slug AS Slug, is_active AS IsActive, environment AS Environment,
                   settings::text AS SettingsJson, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tenant.tenants
            WHERE id = @TenantId
            """,
            new { TenantId = tenantId });
    }

    public async Task<TenantDto?> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var slug = request.Slug.Trim().ToLowerInvariant();
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tenant.tenants WHERE slug = @Slug)",
            new { Slug = slug });

        if (exists)
        {
            return null;
        }

        var tenant = Tenant.Create(request.Name, slug, request.Environment);

        await connection.ExecuteAsync(
            """
            INSERT INTO tenant.tenants (id, name, slug, is_active, environment, settings, created_at, updated_at)
            VALUES (@Id, @Name, @Slug, @IsActive, @Environment, @SettingsJson::jsonb, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.IsActive,
                tenant.Environment,
                tenant.SettingsJson,
                tenant.CreatedAt,
                tenant.UpdatedAt
            });

        await _auditService.LogAsync(
            "tenants.create",
            "tenant",
            tenant.Id.ToString(),
            tenant.Id,
            null,
            new { tenant.Slug },
            cancellationToken);

        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.Environment,
            tenant.SettingsJson,
            tenant.CreatedAt,
            tenant.UpdatedAt);
    }

    public async Task<TenantDto?> UpdateAsync(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(tenantId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var slugConflict = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tenant.tenants WHERE slug = @Slug AND id <> @TenantId)",
            new { Slug = slug, TenantId = tenantId });

        if (slugConflict)
        {
            return null;
        }

        var settingsJson = string.IsNullOrWhiteSpace(request.SettingsJson) ? existing.SettingsJson : request.SettingsJson;
        var now = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(
            """
            UPDATE tenant.tenants
            SET name = @Name, slug = @Slug, is_active = @IsActive, environment = @Environment,
                settings = @SettingsJson::jsonb, updated_at = @Now
            WHERE id = @TenantId
            """,
            new
            {
                request.Name,
                Slug = slug,
                request.IsActive,
                request.Environment,
                SettingsJson = settingsJson,
                Now = now,
                TenantId = tenantId
            });

        await _auditService.LogAsync(
            "tenants.update",
            "tenant",
            tenantId.ToString(),
            tenantId,
            null,
            cancellationToken: cancellationToken);

        return await GetByIdAsync(tenantId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.ExecuteAsync(
            """
            UPDATE tenant.tenants
            SET is_active = false, updated_at = @Now
            WHERE id = @TenantId AND is_active = true
            """,
            new { TenantId = tenantId, Now = DateTimeOffset.UtcNow });

        if (rows == 0)
        {
            return false;
        }

        await _auditService.LogAsync(
            "tenants.delete",
            "tenant",
            tenantId.ToString(),
            tenantId,
            null,
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<TenantMemberDto>> ListMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var members = await connection.QueryAsync<TenantMemberDto>(
            """
            SELECT m.id AS Id, m.tenant_id AS TenantId, m.user_id AS UserId,
                   u.email AS UserEmail, u.display_name AS UserDisplayName,
                   m.role_id AS RoleId, r.name AS RoleName, m.is_active AS IsActive, m.joined_at AS JoinedAt
            FROM tenant.members m
            INNER JOIN identity.users u ON u.id = m.user_id
            INNER JOIN identity.roles r ON r.id = m.role_id
            WHERE m.tenant_id = @TenantId
            ORDER BY u.display_name
            """,
            new { TenantId = tenantId });

        return members.ToList();
    }

    public async Task<TenantMemberDto?> AddMemberAsync(
        Guid tenantId,
        AddTenantMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var userExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM identity.users WHERE id = @UserId)",
            new { request.UserId });

        var roleExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM identity.roles WHERE id = @RoleId)",
            new { request.RoleId });

        if (!userExists || !roleExists)
        {
            return null;
        }

        var existingMember = await connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT id FROM tenant.members WHERE tenant_id = @TenantId AND user_id = @UserId",
            new { TenantId = tenantId, request.UserId });

        if (existingMember.HasValue)
        {
            await connection.ExecuteAsync(
                """
                UPDATE tenant.members
                SET role_id = @RoleId, is_active = true, updated_at = @Now
                WHERE id = @Id
                """,
                new { Id = existingMember.Value, request.RoleId, Now = DateTimeOffset.UtcNow });

            await _rbacService.AssignRoleAsync(request.UserId, tenantId, request.RoleId, cancellationToken);
        }
        else
        {
            var member = TenantMember.Create(tenantId, request.UserId, request.RoleId);
            await connection.ExecuteAsync(
                """
                INSERT INTO tenant.members (id, tenant_id, user_id, role_id, is_active, joined_at, created_at, updated_at)
                VALUES (@Id, @TenantId, @UserId, @RoleId, @IsActive, @JoinedAt, @CreatedAt, @UpdatedAt)
                """,
                new
                {
                    member.Id,
                    member.TenantId,
                    member.UserId,
                    member.RoleId,
                    member.IsActive,
                    member.JoinedAt,
                    member.CreatedAt,
                    member.UpdatedAt
                });

            await _rbacService.AssignRoleAsync(request.UserId, tenantId, request.RoleId, cancellationToken);
        }

        await _auditService.LogAsync(
            "tenants.members.add",
            "tenant_member",
            request.UserId.ToString(),
            tenantId,
            null,
            new { request.RoleId },
            cancellationToken);

        var members = await ListMembersAsync(tenantId, cancellationToken);
        return members.FirstOrDefault(m => m.UserId == request.UserId);
    }

    public async Task<bool> RemoveMemberAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.ExecuteAsync(
            """
            UPDATE tenant.members
            SET is_active = false, updated_at = @Now
            WHERE tenant_id = @TenantId AND user_id = @UserId AND is_active = true
            """,
            new { TenantId = tenantId, UserId = userId, Now = DateTimeOffset.UtcNow });

        if (rows == 0)
        {
            return false;
        }

        await _auditService.LogAsync(
            "tenants.members.remove",
            "tenant_member",
            userId.ToString(),
            tenantId,
            null,
            cancellationToken: cancellationToken);

        return true;
    }
}

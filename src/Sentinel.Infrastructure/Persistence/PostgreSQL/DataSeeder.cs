using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

public sealed class DataSeeder : IDataSeeder
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AdminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid EditorRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid ViewerRoleId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly IPostgreSqlConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;
    private readonly SecurityOptions _securityOptions;

    public DataSeeder(
        IPostgreSqlConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _securityOptions = securityOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var tenantExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tenant.tenants WHERE id = @Id)",
            new { Id = DefaultTenantId });

        if (tenantExists)
        {
            _logger.LogInformation("Seed data already present, skipping");
            return;
        }

        _logger.LogInformation("Seeding default identity and tenant data");

        var now = DateTimeOffset.UtcNow;
        var permissions = CreatePermissions(now);
        var passwordHash = _securityOptions.SeedDefaultAdmin
            ? _passwordHasher.Hash("Admin123!")
            : string.Empty;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
        INSERT INTO tenant.tenants (id, name, slug, is_active, environment, settings, created_at, updated_at)
        VALUES (@Id, @Name, @Slug, true, 'production', '{}', @Now, @Now)
        """,
            new { Id = DefaultTenantId, Name = "Default", Slug = "default", Now = now },
            transaction);

        await connection.ExecuteAsync(
            """
        INSERT INTO identity.roles (id, name, description, is_system, created_at, updated_at)
        VALUES (@Id, @Name, @Description, true, @Now, @Now)
        """,
            new[]
            {
          new { Id = AdminRoleId, Name = "admin", Description = "Full administrative access", Now = now },
          new { Id = EditorRoleId, Name = "editor", Description = "Read and write observability data", Now = now },
          new { Id = ViewerRoleId, Name = "viewer", Description = "Read-only access", Now = now }
            },
            transaction);

        foreach (var permission in permissions)
        {
            await connection.ExecuteAsync(
                """
          INSERT INTO identity.permissions (id, name, resource, action, description, created_at, updated_at)
          VALUES (@Id, @Name, @Resource, @Action, @Description, @CreatedAt, @UpdatedAt)
          """,
                permission,
                transaction);
        }

        var adminPermissionIds = permissions.Select(p => p.Id).ToArray();
        var editorPermissionIds = permissions
            .Where(p => p.Action is "read" or "write")
            .Select(p => p.Id)
            .ToArray();
        var viewerPermissionIds = permissions
            .Where(p => p.Action == "read")
            .Select(p => p.Id)
            .ToArray();

        await SeedRolePermissions(connection, transaction, AdminRoleId, adminPermissionIds);
        await SeedRolePermissions(connection, transaction, EditorRoleId, editorPermissionIds);
        await SeedRolePermissions(connection, transaction, ViewerRoleId, viewerPermissionIds);

        if (_securityOptions.SeedDefaultAdmin)
        {
            await connection.ExecuteAsync(
                """
          INSERT INTO identity.users (id, email, password_hash, display_name, is_active, email_verified, created_at, updated_at)
          VALUES (@Id, @Email, @PasswordHash, @DisplayName, true, true, @Now, @Now)
          """,
                new
                {
                    Id = AdminUserId,
                    Email = "admin@sentinel.local",
                    PasswordHash = passwordHash,
                    DisplayName = "System Administrator",
                    Now = now
                },
                transaction);

            await connection.ExecuteAsync(
                """
          INSERT INTO identity.user_roles (user_id, role_id, tenant_id)
          VALUES (@UserId, @RoleId, @TenantId)
          """,
                new { UserId = AdminUserId, RoleId = AdminRoleId, TenantId = DefaultTenantId },
                transaction);

            await connection.ExecuteAsync(
                """
          INSERT INTO tenant.members (id, tenant_id, user_id, role_id, is_active, joined_at, created_at, updated_at)
          VALUES (@Id, @TenantId, @UserId, @RoleId, true, @Now, @Now, @Now)
          """,
                new
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    TenantId = DefaultTenantId,
                    UserId = AdminUserId,
                    RoleId = AdminRoleId,
                    Now = now
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Seed data created successfully");
    }

    private static List<PermissionSeed> CreatePermissions(DateTimeOffset now)
    {
        var resources = new[]
        {
      "users", "tenants", "logs", "metrics", "traces", "alerts",
      "incidents", "dashboards", "plugins", "settings", "search"
    };

        var permissions = new List<PermissionSeed>();
        var index = 0;

        foreach (var resource in resources)
        {
            foreach (var action in new[] { "read", "write", "delete" })
            {
                if (resource is "search" && action == "delete")
                {
                    continue;
                }

                if (resource is "logs" or "metrics" or "traces" or "search" && action == "delete")
                {
                    continue;
                }

                index++;
                var name = $"{resource}.{action}";
                permissions.Add(new PermissionSeed(
                    Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-{index:D12}"),
                    name,
                    resource,
                    action,
                    $"{action} access to {resource}",
                    now,
                    now));
            }
        }

        permissions.Add(new PermissionSeed(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "tenants.members.manage",
            "tenants",
            "manage",
            "Manage tenant membership",
            now,
            now));

        return permissions;
    }

    private static async Task SeedRolePermissions(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        Guid roleId,
        Guid[] permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            await connection.ExecuteAsync(
                """
          INSERT INTO identity.role_permissions (role_id, permission_id)
          VALUES (@RoleId, @PermissionId)
          """,
                new { RoleId = roleId, PermissionId = permissionId },
                transaction);
        }
    }

    private sealed record PermissionSeed(
        Guid Id,
        string Name,
        string Resource,
        string Action,
        string Description,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

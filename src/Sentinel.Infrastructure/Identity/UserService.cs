using Dapper;
using Sentinel.Domain.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class UserService : IUserService
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public UserService(
        IPostgreSqlConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var users = await connection.QueryAsync<UserDto>(
            """
            SELECT u.id AS Id, u.email AS Email, u.display_name AS DisplayName, u.is_active AS IsActive,
                   u.email_verified AS EmailVerified, u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt, u.updated_at AS UpdatedAt
            FROM identity.users u
            INNER JOIN tenant.members m ON m.user_id = u.id
            WHERE m.tenant_id = @TenantId AND m.is_active = true
            ORDER BY u.display_name
            """,
            new { TenantId = tenantId });

        return users.ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UserDto>(
            """
            SELECT u.id AS Id, u.email AS Email, u.display_name AS DisplayName, u.is_active AS IsActive,
                   u.email_verified AS EmailVerified, u.last_login_at AS LastLoginAt,
                   u.created_at AS CreatedAt, u.updated_at AS UpdatedAt
            FROM identity.users u
            INNER JOIN tenant.members m ON m.user_id = u.id
            WHERE m.tenant_id = @TenantId AND u.id = @UserId AND m.is_active = true
            """,
            new { TenantId = tenantId, UserId = userId });
    }

    public async Task<UserDto?> CreateAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM identity.users WHERE email = @Email)",
            new { Email = normalizedEmail });

        if (exists)
        {
            return null;
        }

        var roleExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM identity.roles WHERE id = @RoleId)",
            new { request.RoleId });

        if (!roleExists)
        {
            return null;
        }

        var user = User.Create(normalizedEmail, _passwordHasher.Hash(request.Password), request.DisplayName);
        var member = TenantMember.Create(tenantId, user.Id, request.RoleId);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO identity.users (id, email, password_hash, display_name, is_active, email_verified, created_at, updated_at)
            VALUES (@Id, @Email, @PasswordHash, @DisplayName, @IsActive, @EmailVerified, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                user.Id,
                user.Email,
                user.PasswordHash,
                user.DisplayName,
                user.IsActive,
                user.EmailVerified,
                user.CreatedAt,
                user.UpdatedAt
            },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO identity.user_roles (user_id, role_id, tenant_id)
            VALUES (@UserId, @RoleId, @TenantId)
            """,
            new { UserId = user.Id, request.RoleId, TenantId = tenantId },
            transaction);

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
            },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        await _auditService.LogAsync(
            "users.create",
            "user",
            user.Id.ToString(),
            tenantId,
            null,
            new { user.Email },
            cancellationToken);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.EmailVerified,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public async Task<UserDto?> UpdateAsync(
        Guid tenantId,
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(tenantId, userId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var passwordHash = _passwordHasher.Hash(request.Password);
            await connection.ExecuteAsync(
                """
                UPDATE identity.users
                SET display_name = @DisplayName, is_active = @IsActive, password_hash = @PasswordHash, updated_at = @Now
                WHERE id = @UserId
                """,
                new
                {
                    request.DisplayName,
                    request.IsActive,
                    PasswordHash = passwordHash,
                    Now = now,
                    UserId = userId
                });
        }
        else
        {
            await connection.ExecuteAsync(
                """
                UPDATE identity.users
                SET display_name = @DisplayName, is_active = @IsActive, updated_at = @Now
                WHERE id = @UserId
                """,
                new { request.DisplayName, request.IsActive, Now = now, UserId = userId });
        }

        await _auditService.LogAsync(
            "users.update",
            "user",
            userId.ToString(),
            tenantId,
            null,
            cancellationToken: cancellationToken);

        return await GetByIdAsync(tenantId, userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
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
            "users.delete",
            "user",
            userId.ToString(),
            tenantId,
            null,
            cancellationToken: cancellationToken);

        return true;
    }
}

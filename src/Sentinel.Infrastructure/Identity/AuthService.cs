using Dapper;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;
using Sentinel.Domain.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRbacService _rbacService;
    private readonly IAuditService _auditService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IPostgreSqlConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRbacService rbacService,
        IAuditService auditService,
        IOptions<JwtOptions> jwtOptions)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _rbacService = rbacService;
        _auditService = auditService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResult?> LoginAsync(
        string email,
        string password,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var user = await connection.QuerySingleOrDefaultAsync<UserRecord>(
            """
            SELECT id, email, password_hash AS PasswordHash, display_name AS DisplayName, is_active AS IsActive
            FROM identity.users
            WHERE email = @Email
            """,
            new { Email = email.Trim().ToLowerInvariant() });

        if (user is null || !user.IsActive || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var resolvedTenantId = tenantId ?? await ResolveDefaultTenantIdAsync(connection, user.Id, cancellationToken);
        if (!resolvedTenantId.HasValue)
        {
            return null;
        }

        var roles = await _rbacService.GetUserRolesAsync(user.Id, resolvedTenantId.Value, cancellationToken);
        if (roles.Count == 0)
        {
            return null;
        }

        var permissions = await _rbacService.GetUserPermissionsAsync(user.Id, resolvedTenantId.Value, cancellationToken);
        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            resolvedTenantId.Value,
            roles,
            permissions);

        var tokenPair = _jwtTokenService.GenerateTokens(authenticatedUser);
        await StoreRefreshTokenAsync(connection, user.Id, tokenPair.RefreshToken, cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE identity.users SET last_login_at = @Now, updated_at = @Now WHERE id = @Id",
            new { Id = user.Id, Now = DateTimeOffset.UtcNow });

        await _auditService.LogAsync(
            "auth.login",
            "user",
            user.Id.ToString(),
            resolvedTenantId.Value,
            user.Id,
            cancellationToken: cancellationToken);

        return CreateAuthResult(authenticatedUser, tokenPair);
    }

    public async Task<AuthResult?> RegisterAsync(
        string email,
        string password,
        string displayName,
        string tenantName,
        string tenantSlug,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM identity.users WHERE email = @Email)",
            new { Email = normalizedEmail });

        if (existingUser)
        {
            return null;
        }

        var normalizedSlug = tenantSlug.Trim().ToLowerInvariant();
        var existingTenant = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tenant.tenants WHERE slug = @Slug)",
            new { Slug = normalizedSlug });

        if (existingTenant)
        {
            return null;
        }

        var user = User.Create(normalizedEmail, _passwordHasher.Hash(password), displayName);
        var tenant = Tenant.Create(tenantName, normalizedSlug);
        var now = DateTimeOffset.UtcNow;

        var adminRoleId = await connection.QuerySingleAsync<Guid>(
            "SELECT id FROM identity.roles WHERE name = 'admin'");

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

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
            },
            transaction);

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
            new { UserId = user.Id, RoleId = adminRoleId, TenantId = tenant.Id },
            transaction);

        var member = TenantMember.Create(tenant.Id, user.Id, adminRoleId);
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

        var roles = await _rbacService.GetUserRolesAsync(user.Id, tenant.Id, cancellationToken);
        var permissions = await _rbacService.GetUserPermissionsAsync(user.Id, tenant.Id, cancellationToken);
        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            tenant.Id,
            roles,
            permissions);

        var tokenPair = _jwtTokenService.GenerateTokens(authenticatedUser);
        await StoreRefreshTokenAsync(connection, user.Id, tokenPair.RefreshToken, cancellationToken);

        await _auditService.LogAsync(
            "auth.register",
            "user",
            user.Id.ToString(),
            tenant.Id,
            user.Id,
            new { tenant.Slug },
            cancellationToken);

        return CreateAuthResult(authenticatedUser, tokenPair);
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var storedToken = await connection.QuerySingleOrDefaultAsync<RefreshTokenRecord>(
            """
            SELECT id AS Id, user_id AS UserId, expires_at AS ExpiresAt, revoked_at AS RevokedAt
            FROM identity.refresh_tokens
            WHERE token_hash = @TokenHash
            """,
            new { TokenHash = tokenHash });

        if (storedToken is null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var user = await connection.QuerySingleOrDefaultAsync<UserRecord>(
            """
            SELECT id, email, password_hash AS PasswordHash, display_name AS DisplayName, is_active AS IsActive
            FROM identity.users
            WHERE id = @Id
            """,
            new { Id = storedToken.UserId });

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var tenantId = await ResolveDefaultTenantIdAsync(connection, user.Id, cancellationToken);
        if (!tenantId.HasValue)
        {
            return null;
        }

        var roles = await _rbacService.GetUserRolesAsync(user.Id, tenantId.Value, cancellationToken);
        var permissions = await _rbacService.GetUserPermissionsAsync(user.Id, tenantId.Value, cancellationToken);
        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            tenantId.Value,
            roles,
            permissions);

        var tokenPair = _jwtTokenService.GenerateTokens(authenticatedUser);

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            _jwtTokenService.HashRefreshToken(tokenPair.RefreshToken),
            DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays));

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            UPDATE identity.refresh_tokens
            SET revoked_at = @RevokedAt, replaced_by_token_id = @ReplacedByTokenId, updated_at = @UpdatedAt
            WHERE id = @Id
            """,
            new
            {
                Id = storedToken.Id,
                RevokedAt = DateTimeOffset.UtcNow,
                ReplacedByTokenId = newRefreshToken.Id,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO identity.refresh_tokens (id, user_id, token_hash, expires_at, created_at, updated_at)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                newRefreshToken.Id,
                newRefreshToken.UserId,
                TokenHash = newRefreshToken.TokenHash,
                newRefreshToken.ExpiresAt,
                newRefreshToken.CreatedAt,
                newRefreshToken.UpdatedAt
            },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return CreateAuthResult(authenticatedUser, tokenPair);
    }

    private async Task StoreRefreshTokenAsync(
        Npgsql.NpgsqlConnection connection,
        Guid userId,
        string refreshTokenValue,
        CancellationToken cancellationToken)
    {
        var refreshToken = RefreshToken.Create(
            userId,
            _jwtTokenService.HashRefreshToken(refreshTokenValue),
            DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays));

        await connection.ExecuteAsync(
            """
            INSERT INTO identity.refresh_tokens (id, user_id, token_hash, expires_at, created_at, updated_at)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                refreshToken.Id,
                refreshToken.UserId,
                TokenHash = refreshToken.TokenHash,
                refreshToken.ExpiresAt,
                refreshToken.CreatedAt,
                refreshToken.UpdatedAt
            });
    }

    private static async Task<Guid?> ResolveDefaultTenantIdAsync(
        Npgsql.NpgsqlConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<Guid?>(
            """
            SELECT m.tenant_id
            FROM tenant.members m
            WHERE m.user_id = @UserId AND m.is_active = true
            ORDER BY m.joined_at
            LIMIT 1
            """,
            new { UserId = userId });
    }

    private static AuthResult CreateAuthResult(AuthenticatedUser user, TokenPair tokenPair)
    {
        return new AuthResult(
            new AuthTokens(tokenPair.AccessToken, tokenPair.RefreshToken, tokenPair.AccessTokenExpiresAt),
            new AuthUserInfo(
                user.UserId,
                user.Email,
                user.DisplayName,
                user.TenantId,
                user.Roles,
                user.Permissions));
    }

    private sealed record UserRecord(
        Guid Id,
        string Email,
        string PasswordHash,
        string DisplayName,
        bool IsActive);

    private sealed record RefreshTokenRecord(
        Guid Id,
        Guid UserId,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? RevokedAt);
}

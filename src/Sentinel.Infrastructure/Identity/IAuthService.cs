namespace Sentinel.Infrastructure.Identity;

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt);

public sealed record AuthUserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResult(AuthTokens Tokens, AuthUserInfo User);

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password, Guid? tenantId, CancellationToken cancellationToken = default);
    Task<AuthResult?> RegisterAsync(string email, string password, string displayName, string tenantName, string tenantSlug, CancellationToken cancellationToken = default);
    Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

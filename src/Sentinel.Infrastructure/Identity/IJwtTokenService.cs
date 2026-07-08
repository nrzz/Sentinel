using System.Security.Claims;

namespace Sentinel.Infrastructure.Identity;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public interface IJwtTokenService
{
    TokenPair GenerateTokens(AuthenticatedUser user);
    ClaimsPrincipal? ValidateAccessToken(string accessToken);
    string GenerateRefreshTokenValue();
    string HashRefreshToken(string refreshToken);
}

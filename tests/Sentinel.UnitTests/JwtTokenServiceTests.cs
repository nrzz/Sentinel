using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly JwtOptions _options;

    public JwtTokenServiceTests()
    {
        _options = new JwtOptions
        {
            Issuer = "sentinel-test",
            Audience = "sentinel-api-test",
            SecretKey = "unit-test-jwt-secret-key-min-32-chars",
            AccessTokenExpirationMinutes = 60,
        };

        _service = new JwtTokenService(Options.Create(_options));
    }

    private static AuthenticatedUser CreateTestUser() => new(
        UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email: "test@sentinel.local",
        DisplayName: "Test User",
        TenantId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Roles: ["admin", "viewer"],
        Permissions: ["logs:read", "logs:write"]);

    [Fact]
    public void GenerateTokens_ReturnsValidTokenPair()
    {
        var user = CreateTestUser();
        var tokens = _service.GenerateTokens(user);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ValidateAccessToken_ReturnsPrincipalForValidToken()
    {
        var user = CreateTestUser();
        var tokens = _service.GenerateTokens(user);

        var principal = _service.ValidateAccessToken(tokens.AccessToken);

        Assert.NotNull(principal);
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal(user.UserId.ToString(), sub);

        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        Assert.Equal(user.Email, email);

        Assert.Equal(user.TenantId.ToString(), principal.FindFirst("tenant_id")?.Value);
    }

    [Fact]
    public void ValidateAccessToken_ReturnsNullForInvalidToken()
    {
        var principal = _service.ValidateAccessToken("not.a.valid.jwt.token");

        Assert.Null(principal);
    }

    [Fact]
    public void GenerateTokens_IncludesRolesAndPermissions()
    {
        var user = CreateTestUser();
        var tokens = _service.GenerateTokens(user);
        var principal = _service.ValidateAccessToken(tokens.AccessToken)!;

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = principal.FindAll("permission").Select(c => c.Value).ToList();

        Assert.Contains("admin", roles);
        Assert.Contains("viewer", roles);
        Assert.Contains("logs:read", permissions);
        Assert.Contains("logs:write", permissions);
    }

    [Fact]
    public void GenerateRefreshTokenValue_ReturnsUniqueValues()
    {
        var token1 = _service.GenerateRefreshTokenValue();
        var token2 = _service.GenerateRefreshTokenValue();

        Assert.NotEqual(token1, token2);
        Assert.True(token1.Length > 20);
    }

    [Fact]
    public void HashRefreshToken_ReturnsConsistentHash()
    {
        const string refreshToken = "test-refresh-token-value";

        var hash1 = _service.HashRefreshToken(refreshToken);
        var hash2 = _service.HashRefreshToken(refreshToken);

        Assert.Equal(hash1, hash2);
        Assert.True(hash1.Length > 0);
    }

    [Fact]
    public void HashRefreshToken_DifferentTokensProduceDifferentHashes()
    {
        var hash1 = _service.HashRefreshToken("token-a");
        var hash2 = _service.HashRefreshToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }
}

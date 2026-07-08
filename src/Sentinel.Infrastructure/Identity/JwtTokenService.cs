using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenPair GenerateTokens(AuthenticatedUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new("tenant_id", user.TenantId.ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.Permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = _tokenHandler.WriteToken(token);
        var refreshToken = GenerateRefreshTokenValue();

        return new TokenPair(accessToken, refreshToken, expiresAt);
    }

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            return _tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}

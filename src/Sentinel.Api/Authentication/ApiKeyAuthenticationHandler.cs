using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyRepository _apiKeyRepository;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeyRepository)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryGetApiKey(out var apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var keyHash = HashApiKey(apiKey);
        var storedKey = await _apiKeyRepository.FindByHashAsync(keyHash, Context.RequestAborted);
        if (storedKey is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        await _apiKeyRepository.UpdateLastUsedAsync(storedKey.Id, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new("tenant_id", storedKey.TenantId.ToString()),
            new(ClaimTypes.NameIdentifier, storedKey.Id.ToString()),
            new(ClaimTypes.Name, storedKey.Name),
        };

        foreach (var scope in storedKey.Scopes)
        {
            claims.Add(new Claim("permission", scope));
        }

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private bool TryGetApiKey(out string apiKey)
    {
        apiKey = string.Empty;

        if (Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            apiKey = headerValue.ToString();
            return true;
        }

        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization["Bearer ".Length..].Trim();
            if (token.StartsWith("sent_", StringComparison.Ordinal))
            {
                apiKey = token;
                return true;
            }
        }

        return false;
    }

    internal static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

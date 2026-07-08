namespace Sentinel.PluginSdk;

public sealed record AuthCredentials(string Username, string Password, IReadOnlyDictionary<string, string>? Extra = null);

public sealed record AuthTokenResult(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string>? Claims);

public sealed record AuthValidationResult(
    bool IsValid,
    string? Subject,
    IReadOnlyDictionary<string, string>? Claims,
    string? Error);

/// <summary>Integrates external identity providers with Sentinel authentication.</summary>
public interface IAuthenticationProvider : IPluginMetadata
{
    Task<AuthTokenResult> AuthenticateAsync(
        AuthCredentials credentials,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task<AuthValidationResult> ValidateTokenAsync(
        string accessToken,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResult> RefreshAsync(
        string refreshToken,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}

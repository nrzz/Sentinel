namespace Sentinel.Api.Features.Authentication;

public sealed record LoginRequest(string Email, string Password, Guid? TenantId);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string TenantName,
    string TenantSlug);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    int ExpiresIn,
    AuthUserResponse User);

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

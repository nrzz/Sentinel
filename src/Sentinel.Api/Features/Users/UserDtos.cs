namespace Sentinel.Api.Features.Users;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailVerified,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateUserApiRequest(
    string Email,
    string Password,
    string DisplayName,
    Guid RoleId);

public sealed record UpdateUserApiRequest(
    string DisplayName,
    bool IsActive,
    string? Password);

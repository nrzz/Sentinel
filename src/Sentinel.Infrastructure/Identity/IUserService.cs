namespace Sentinel.Infrastructure.Identity;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailVerified,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string DisplayName,
    Guid RoleId);

public sealed record UpdateUserRequest(
    string DisplayName,
    bool IsActive,
    string? Password);

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> CreateAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}

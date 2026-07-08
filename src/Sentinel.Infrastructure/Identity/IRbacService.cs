namespace Sentinel.Infrastructure.Identity;

public interface IRbacService
{
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string permission, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid tenantId, Guid roleId, CancellationToken cancellationToken = default);
}

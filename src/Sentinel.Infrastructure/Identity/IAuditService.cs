namespace Sentinel.Infrastructure.Identity;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string resourceType,
        string? resourceId,
        Guid? tenantId,
        Guid? userId,
        object? details = null,
        CancellationToken cancellationToken = default);
}

using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class AuditEvent : Entity
{
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public string DetailsJson { get; private set; } = "{}";
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string Environment { get; private set; } = "production";

    private AuditEvent()
    {
    }

    public static AuditEvent Create(
        string action,
        string resourceType,
        string? resourceId,
        Guid? tenantId,
        Guid? userId,
        string detailsJson,
        string? ipAddress,
        string? userAgent,
        string environment = "production")
    {
        return new AuditEvent
        {
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            TenantId = tenantId,
            UserId = userId,
            DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Environment = environment
        };
    }
}

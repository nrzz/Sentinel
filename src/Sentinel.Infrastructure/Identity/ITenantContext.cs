namespace Sentinel.Infrastructure.Identity;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool HasTenant { get; }
    Guid RequireTenantId();
    void SetTenantId(Guid tenantId);
}

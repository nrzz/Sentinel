using Sentinel.Domain.Common;

namespace Sentinel.Domain.Identity;

public sealed class TenantMember : Entity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset JoinedAt { get; private set; } = DateTimeOffset.UtcNow;

    private TenantMember()
    {
    }

    public static TenantMember Create(Guid tenantId, Guid userId, Guid roleId)
    {
        return new TenantMember
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            JoinedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateRole(Guid roleId)
    {
        RoleId = roleId;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }
}

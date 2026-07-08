using Sentinel.Domain.Common;

namespace Sentinel.Domain.Dashboards;

public sealed class Dashboard : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string LayoutJson { get; private set; } = "{}";
    public bool IsDefault { get; private set; }
    public string? CreatedBy { get; private set; }

    private Dashboard() { }

    public static Dashboard Create(
        Guid tenantId,
        string name,
        string description,
        string layoutJson,
        bool isDefault = false,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutJson);

        return new Dashboard
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description.Trim(),
            LayoutJson = layoutJson,
            IsDefault = isDefault,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string description, string layoutJson, bool isDefault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutJson);

        Name = name.Trim();
        Description = description.Trim();
        LayoutJson = layoutJson;
        IsDefault = isDefault;
        Touch();
    }

    public static Dashboard FromPersistence(
        Guid id,
        Guid tenantId,
        string name,
        string description,
        string layoutJson,
        bool isDefault,
        string? createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Description = description,
            LayoutJson = layoutJson,
            IsDefault = isDefault,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

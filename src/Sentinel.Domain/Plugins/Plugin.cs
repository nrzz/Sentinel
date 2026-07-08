using Sentinel.Domain.Common;

namespace Sentinel.Domain.Plugins;

public enum PluginStatus
{
    Installed = 0,
    Enabled = 1,
    Disabled = 2,
    Failed = 3
}

public sealed class Plugin : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string AssemblyName { get; private set; } = string.Empty;
    public string ConfigurationJson { get; private set; } = "{}";
    public PluginStatus Status { get; private set; }
    public string? InstalledBy { get; private set; }
    public DateTimeOffset InstalledAt { get; private set; }

    private Plugin() { }

    public static Plugin Create(
        Guid tenantId,
        string name,
        string version,
        string description,
        string assemblyName,
        string configurationJson,
        string? installedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyName);

        return new Plugin
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Version = version.Trim(),
            Description = description.Trim(),
            AssemblyName = assemblyName.Trim(),
            ConfigurationJson = configurationJson,
            Status = PluginStatus.Installed,
            InstalledBy = installedBy,
            InstalledAt = DateTimeOffset.UtcNow
        };
    }

    public void Enable()
    {
        Status = PluginStatus.Enabled;
        Touch();
    }

    public void Disable()
    {
        Status = PluginStatus.Disabled;
        Touch();
    }

    public void MarkFailed(string reason)
    {
        Description = $"{Description} (error: {reason})";
        Status = PluginStatus.Failed;
        Touch();
    }

    public void UpdateConfiguration(string configurationJson)
    {
        ConfigurationJson = configurationJson;
        Touch();
    }

    public static Plugin FromPersistence(
        Guid id,
        Guid tenantId,
        string name,
        string version,
        string description,
        string assemblyName,
        string configurationJson,
        PluginStatus status,
        string? installedBy,
        DateTimeOffset installedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Version = version,
            Description = description,
            AssemblyName = assemblyName,
            ConfigurationJson = configurationJson,
            Status = status,
            InstalledBy = installedBy,
            InstalledAt = installedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

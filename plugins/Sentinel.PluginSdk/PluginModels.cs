namespace Sentinel.PluginSdk;

public enum PluginHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record ConfigSchemaProperty(
    string Name,
    string Type,
    string Description,
    bool Required,
    object? DefaultValue = null,
    IReadOnlyList<string>? AllowedValues = null);

public sealed record PluginHealthResult(
    PluginHealthStatus Status,
    string Message,
    IReadOnlyDictionary<string, string>? Details = null);

public sealed record PluginConfigurationContext(
    Guid TenantId,
    IReadOnlyDictionary<string, string> Settings);

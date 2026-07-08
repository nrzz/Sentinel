namespace Sentinel.PluginSdk;

/// <summary>Base class for Sentinel plugins providing metadata, configuration, and health checks.</summary>
public abstract class PluginBase : IPluginMetadata
{
    private IReadOnlyDictionary<string, string> _settings = new Dictionary<string, string>();

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Description { get; }
    public virtual string Author => "Sentinel";
    public virtual IReadOnlyList<string> Tags => [];

    public abstract IReadOnlyDictionary<string, ConfigSchemaProperty> GetConfigSchema();

    public virtual PluginHealthResult CheckHealth() =>
        new(PluginHealthStatus.Healthy, "Plugin is operational.");

    public virtual void Configure(PluginConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _settings = context.Settings;
        OnConfigure(context);
    }

    protected IReadOnlyDictionary<string, string> Settings => _settings;

    protected virtual void OnConfigure(PluginConfigurationContext context)
    {
    }

    protected string GetRequiredSetting(string key)
    {
        if (!Settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required plugin setting '{key}' is missing or empty.");
        }

        return value;
    }

    protected string GetSetting(string key, string defaultValue) =>
        Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
}

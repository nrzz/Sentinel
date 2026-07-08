namespace Sentinel.PluginSdk;

/// <summary>Describes a Sentinel plugin identity and presentation metadata.</summary>
public interface IPluginMetadata
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }
    IReadOnlyList<string> Tags { get; }
}

namespace Sentinel.PluginSdk;

public sealed record StorageObject(
    string Key,
    ReadOnlyMemory<byte> Content,
    string ContentType,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record StorageListResult(
    IReadOnlyList<string> Keys,
    string? ContinuationToken);

/// <summary>Stores and retrieves binary objects for Sentinel plugins and exports.</summary>
public interface IStorageProviderPlugin : IPluginMetadata
{
    Task PutAsync(
        string key,
        StorageObject obj,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task<StorageObject?> GetAsync(
        string key,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string key,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task<StorageListResult> ListAsync(
        string prefix,
        int maxKeys,
        string? continuationToken,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}

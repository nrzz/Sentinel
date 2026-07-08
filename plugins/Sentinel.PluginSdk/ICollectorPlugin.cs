namespace Sentinel.PluginSdk;

public sealed record CollectedLogEntry(
    string Service,
    string Environment,
    string Level,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string>? Attributes = null,
    string? TraceId = null,
    string? SpanId = null,
    string? CorrelationId = null,
    string? SourcePath = null);

public sealed record CollectorContext(
    Guid TenantId,
    DateTimeOffset? Since,
    int MaxEntries,
    IReadOnlyDictionary<string, string> Settings);

public sealed record CollectorResult(
    IReadOnlyList<CollectedLogEntry> Entries,
    bool HasMore,
    DateTimeOffset? NextCursor);

/// <summary>Ingests log data from external sources into Sentinel.</summary>
public interface ICollectorPlugin : IPluginMetadata
{
    Task<CollectorResult> CollectAsync(CollectorContext context, CancellationToken cancellationToken = default);
}

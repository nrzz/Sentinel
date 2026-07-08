namespace Sentinel.Domain.Events;

public sealed record LogEnriched(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string NormalizedLevel,
    string Message,
    IReadOnlyDictionary<string, string> Attributes,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    string? ParsedException,
    string? SourceHost,
    DateTimeOffset IngestedAt,
    DateTimeOffset EnrichedAt);

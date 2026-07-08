namespace Sentinel.Domain.Observability;

public sealed record LogRecord(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string Message,
    string AttributesJson,
    string? TraceId,
    string? SpanId,
    string? CorrelationId);

public sealed record EnrichedLogRecord(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string NormalizedLevel,
    string Message,
    string AttributesJson,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    string? ParsedException,
    string? SourceHost,
    DateTimeOffset IngestedAt,
    DateTimeOffset EnrichedAt);

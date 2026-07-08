namespace Sentinel.Domain.Observability;

public sealed record TraceRecord(
    Guid Id,
    Guid TenantId,
    string TraceId,
    DateTimeOffset Timestamp,
    string Service,
    string Name,
    double DurationMs,
    string Status,
    string AttributesJson);

public sealed record TraceSpanRecord(
    Guid Id,
    Guid TenantId,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    DateTimeOffset Timestamp,
    string Name,
    string Service,
    double DurationMs,
    string Status,
    string AttributesJson);

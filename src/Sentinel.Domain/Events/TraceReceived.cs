namespace Sentinel.Domain.Events;

public sealed record TraceReceived(
    Guid Id,
    Guid TenantId,
    string TraceId,
    DateTimeOffset Timestamp,
    string Service,
    string Name,
    double DurationMs,
    string Status,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<TraceSpanReceived> Spans,
    DateTimeOffset ReceivedAt);

public sealed record TraceSpanReceived(
    string SpanId,
    string? ParentSpanId,
    DateTimeOffset Timestamp,
    string Name,
    string Service,
    double DurationMs,
    string Status,
    IReadOnlyDictionary<string, string> Attributes);

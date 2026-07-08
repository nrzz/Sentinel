namespace Sentinel.Domain.Events;

public sealed record LogReceived(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string Message,
    IReadOnlyDictionary<string, string> Attributes,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    DateTimeOffset ReceivedAt);

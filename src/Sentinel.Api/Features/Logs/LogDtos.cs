namespace Sentinel.Api.Features.Logs;

public sealed record LogEntryDto(
    string Service,
    string Environment,
    string Level,
    string Message,
    Dictionary<string, string>? Attributes,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    DateTimeOffset? Timestamp);

public sealed record IngestLogsRequest(IReadOnlyList<LogEntryDto> Logs);

public sealed record IngestLogsResponse(int AcceptedCount, string Status);

public sealed record LogStreamDto(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string Message,
    string? TraceId,
    string? CorrelationId);

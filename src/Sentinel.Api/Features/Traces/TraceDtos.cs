namespace Sentinel.Api.Features.Traces;

public sealed record SpanEntryDto(
    string SpanId,
    string? ParentSpanId,
    string Name,
    string Service,
    double DurationMs,
    string Status,
    Dictionary<string, string>? Attributes,
    DateTimeOffset? Timestamp);

public sealed record TraceEntryDto(
    string TraceId,
    string Service,
    string Name,
    double DurationMs,
    string Status,
    Dictionary<string, string>? Attributes,
    IReadOnlyList<SpanEntryDto>? Spans,
    DateTimeOffset? Timestamp);

public sealed record IngestTracesRequest(IReadOnlyList<TraceEntryDto> Traces);

public sealed record IngestTracesResponse(int AcceptedCount, string Status);

public sealed record QueryTracesRequest(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Service,
    string? TraceId,
    string? Status,
    int Limit = 100);

public sealed record TraceItemDto(
    Guid Id,
    string TraceId,
    DateTimeOffset Timestamp,
    string Service,
    string Name,
    double DurationMs,
    string Status);

public sealed record TraceSpanItemDto(
    Guid Id,
    string SpanId,
    string? ParentSpanId,
    DateTimeOffset Timestamp,
    string Name,
    string Service,
    double DurationMs,
    string Status);

public sealed record QueryTracesResponse(IReadOnlyList<TraceItemDto> Items);

public sealed record GetTraceDetailResponse(TraceItemDto Trace, IReadOnlyList<TraceSpanItemDto> Spans);

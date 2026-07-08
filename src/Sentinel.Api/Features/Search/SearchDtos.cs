namespace Sentinel.Api.Features.Search;

public sealed record SearchLogsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Level,
    string? Service,
    string? Environment,
    string? Query,
    string? TraceId,
    int Limit = 100,
    int Offset = 0);

public sealed record LogSearchItemDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string NormalizedLevel,
    string Message,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    string? ParsedException,
    string? SourceHost);

public sealed record SearchLogsResponse(
    IReadOnlyList<LogSearchItemDto> Items,
    long TotalCount,
    int Limit,
    int Offset);

public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    string Query,
    string? Level,
    string? Service,
    string? Environment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateSavedSearchRequest(
    string Name,
    string Query,
    string? Level,
    string? Service,
    string? Environment);

public sealed record UpdateSavedSearchRequest(
    string Name,
    string Query,
    string? Level,
    string? Service,
    string? Environment);

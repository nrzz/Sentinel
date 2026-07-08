namespace Sentinel.Api.Features.Metrics;

public sealed record MetricEntryDto(
    string Name,
    double Value,
    string Unit,
    Dictionary<string, string>? Tags,
    string Service,
    string Environment,
    DateTimeOffset? Timestamp);

public sealed record IngestMetricsRequest(IReadOnlyList<MetricEntryDto> Metrics);

public sealed record IngestMetricsResponse(int AcceptedCount, string Status);

public sealed record QueryMetricsRequest(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Name,
    string? Service,
    string? Environment,
    int Limit = 1000);

public sealed record MetricItemDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Name,
    double Value,
    string Unit,
    string Service,
    string Environment,
    Dictionary<string, string> Tags);

public sealed record QueryMetricsResponse(IReadOnlyList<MetricItemDto> Items);

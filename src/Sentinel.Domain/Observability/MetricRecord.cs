namespace Sentinel.Domain.Observability;

public sealed record MetricRecord(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Name,
    double Value,
    string Unit,
    string TagsJson,
    string Service,
    string Environment);

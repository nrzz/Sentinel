namespace Sentinel.Domain.Events;

public sealed record AnomalyDetected(
    Guid Id,
    Guid TenantId,
    string AnomalyType,
    string SourceType,
    string SourceId,
    double Score,
    string Description,
    DateTimeOffset DetectedAt);

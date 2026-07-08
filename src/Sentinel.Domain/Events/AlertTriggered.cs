namespace Sentinel.Domain.Events;

public sealed record AlertTriggered(
    Guid Id,
    Guid TenantId,
    string RuleName,
    string Severity,
    string Message,
    string SourceType,
    string SourceId,
    double? ThresholdValue,
    double? ActualValue,
    DateTimeOffset TriggeredAt);

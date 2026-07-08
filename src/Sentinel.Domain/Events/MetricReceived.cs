namespace Sentinel.Domain.Events;

public sealed record MetricReceived(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Name,
    double Value,
    string Unit,
    IReadOnlyDictionary<string, string> Tags,
    string Service,
    string Environment,
    DateTimeOffset ReceivedAt);

namespace Sentinel.Domain.Events;

public sealed record CorrelationDiscovered(
    Guid Id,
    Guid TenantId,
    string CorrelationType,
    IReadOnlyList<string> RelatedIds,
    double Confidence,
    string Description,
    DateTimeOffset DiscoveredAt);

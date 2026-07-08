namespace Sentinel.Domain.Events;

public sealed record DeploymentReceived(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Version,
    string Environment,
    string Status,
    string CommitSha,
    string Repository,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset ReceivedAt);

namespace Sentinel.Domain.Observability;

public sealed record DeploymentRecord(
    Guid Id,
    Guid TenantId,
    DateTimeOffset Timestamp,
    string Service,
    string Version,
    string Environment,
    string Status,
    string CommitSha,
    string Repository,
    string MetadataJson);

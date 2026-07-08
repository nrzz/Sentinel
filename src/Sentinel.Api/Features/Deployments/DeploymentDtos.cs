namespace Sentinel.Api.Features.Deployments;

public sealed record DeploymentWebhookRequest(
    string Service,
    string Version,
    string Environment,
    string Status,
    string CommitSha,
    string Repository,
    Dictionary<string, string>? Metadata,
    DateTimeOffset? Timestamp);

public sealed record DeploymentWebhookResponse(Guid Id, string Status);

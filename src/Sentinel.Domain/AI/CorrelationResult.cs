namespace Sentinel.Domain.AI;

public sealed class CorrelationResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TenantId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public IReadOnlyList<CorrelationMatch> Matches { get; init; } = [];
    public IReadOnlyList<CorrelationSource> Sources { get; init; } = [];
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class CorrelationMatch
{
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Relationship { get; init; } = string.Empty;
    public double Score { get; init; }
    public string? Description { get; init; }
}

public sealed class CorrelationSource
{
    public string SourceType { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public string Citation { get; init; } = string.Empty;
}

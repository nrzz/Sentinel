using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed record MetricQueryFilters(
    Guid TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Name,
    string? Service,
    string? Environment,
    int Limit = 1000);

public interface IMetricRepository
{
    Task BatchInsertAsync(IReadOnlyList<MetricRecord> metrics, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricRecord>> QueryAsync(MetricQueryFilters filters, CancellationToken cancellationToken = default);
}

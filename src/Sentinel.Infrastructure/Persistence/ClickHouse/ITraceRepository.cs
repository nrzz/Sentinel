using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed record TraceQueryFilters(
    Guid TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Service,
    string? TraceId,
    string? Status,
    int Limit = 100);

public interface ITraceRepository
{
    Task BatchInsertTracesAsync(IReadOnlyList<TraceRecord> traces, CancellationToken cancellationToken = default);
    Task BatchInsertSpansAsync(IReadOnlyList<TraceSpanRecord> spans, CancellationToken cancellationToken = default);
    Task BatchInsertDeploymentsAsync(IReadOnlyList<DeploymentRecord> deployments, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TraceRecord>> QueryTracesAsync(TraceQueryFilters filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TraceSpanRecord>> GetSpansByTraceIdAsync(Guid tenantId, string traceId, CancellationToken cancellationToken = default);
}

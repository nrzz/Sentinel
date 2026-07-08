using Sentinel.Domain.Dashboards;

namespace Sentinel.Infrastructure.Dashboards;

public interface IDashboardRepository
{
    Task<Dashboard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dashboard>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task CreateAsync(Dashboard dashboard, CancellationToken cancellationToken = default);
    Task UpdateAsync(Dashboard dashboard, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}

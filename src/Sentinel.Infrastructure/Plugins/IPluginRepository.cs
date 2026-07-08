using Sentinel.Domain.Plugins;

namespace Sentinel.Infrastructure.Plugins;

public interface IPluginRepository
{
    Task<Plugin?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<Plugin?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Plugin>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task InstallAsync(Plugin plugin, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}

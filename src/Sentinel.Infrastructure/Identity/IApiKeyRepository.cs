using Sentinel.Domain.Identity;

namespace Sentinel.Infrastructure.Identity;

public interface IApiKeyRepository
{
    Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default);
}

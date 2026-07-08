namespace Sentinel.Infrastructure.Persistence.Search;

public sealed record SavedSearch(
    Guid Id,
    Guid TenantId,
    string Name,
    string Query,
    string? Level,
    string? Service,
    string? Environment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface ISavedSearchRepository
{
    Task<IReadOnlyList<SavedSearch>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SavedSearch?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<SavedSearch> CreateAsync(SavedSearch search, CancellationToken cancellationToken = default);
    Task<SavedSearch?> UpdateAsync(SavedSearch search, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}

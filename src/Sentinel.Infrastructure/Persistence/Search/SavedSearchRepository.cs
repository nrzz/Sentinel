using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sentinel.Infrastructure.Persistence.Redis;
using StackExchange.Redis;

namespace Sentinel.Infrastructure.Persistence.Search;

public sealed class SavedSearchRepository : ISavedSearchRepository
{
    private readonly IRedisConnectionFactory _redisConnectionFactory;
    private readonly ILogger<SavedSearchRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SavedSearchRepository(IRedisConnectionFactory redisConnectionFactory, ILogger<SavedSearchRepository> logger)
    {
        _redisConnectionFactory = redisConnectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SavedSearch>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connection = await _redisConnectionFactory.GetConnectionAsync();
        var db = connection.GetDatabase();
        var key = IndexKey(tenantId);
        var members = await db.SetMembersAsync(key);

        var results = new List<SavedSearch>();
        foreach (var member in members)
        {
            var search = await GetFromRedisAsync(db, tenantId, member.ToString());
            if (search is not null)
            {
                results.Add(search);
            }
        }

        return results.OrderBy(s => s.Name).ToList();
    }

    public async Task<SavedSearch?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _redisConnectionFactory.GetConnectionAsync();
        var db = connection.GetDatabase();
        return await GetFromRedisAsync(db, tenantId, id.ToString());
    }

    public async Task<SavedSearch> CreateAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        var connection = await _redisConnectionFactory.GetConnectionAsync();
        var db = connection.GetDatabase();
        var dataKey = DataKey(search.TenantId, search.Id);
        var json = JsonSerializer.Serialize(search, JsonOptions);

        await db.StringSetAsync(dataKey, json);
        await db.SetAddAsync(IndexKey(search.TenantId), search.Id.ToString());

        _logger.LogDebug("Created saved search {SearchId} for tenant {TenantId}", search.Id, search.TenantId);
        return search;
    }

    public async Task<SavedSearch?> UpdateAsync(SavedSearch search, CancellationToken cancellationToken = default)
    {
        var connection = await _redisConnectionFactory.GetConnectionAsync();
        var db = connection.GetDatabase();
        var dataKey = DataKey(search.TenantId, search.Id);

        if (!await db.KeyExistsAsync(dataKey))
        {
            return null;
        }

        var json = JsonSerializer.Serialize(search, JsonOptions);
        await db.StringSetAsync(dataKey, json);
        return search;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await _redisConnectionFactory.GetConnectionAsync();
        var db = connection.GetDatabase();
        var dataKey = DataKey(tenantId, id);
        var removed = await db.KeyDeleteAsync(dataKey);
        await db.SetRemoveAsync(IndexKey(tenantId), id.ToString());
        return removed;
    }

    private static async Task<SavedSearch?> GetFromRedisAsync(IDatabase db, Guid tenantId, string id)
    {
        if (!Guid.TryParse(id, out var searchId))
        {
            return null;
        }

        var value = await db.StringGetAsync(DataKey(tenantId, searchId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<SavedSearch>(value.ToString(), JsonOptions);
    }

    private static string IndexKey(Guid tenantId) => $"sentinel:saved-searches:{tenantId}";
    private static string DataKey(Guid tenantId, Guid id) => $"sentinel:saved-search:{tenantId}:{id}";
}

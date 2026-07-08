using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;
using StackExchange.Redis;

namespace Sentinel.Infrastructure.Persistence.Redis;

public interface IRedisConnectionFactory
{
    Task<IConnectionMultiplexer> GetConnectionAsync();
}

public sealed class RedisConnectionFactory : IRedisConnectionFactory
{
    private readonly RedisOptions _options;
    private IConnectionMultiplexer? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RedisConnectionFactory(IOptions<RedisOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IConnectionMultiplexer> GetConnectionAsync()
    {
        if (_connection is not null && _connection.IsConnected)
        {
            return _connection;
        }

        await _lock.WaitAsync();
        try
        {
            if (_connection is not null && _connection.IsConnected)
            {
                return _connection;
            }

            _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}

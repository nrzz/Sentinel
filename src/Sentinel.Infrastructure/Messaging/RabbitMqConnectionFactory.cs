using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.Messaging;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null && _connection.IsOpen)
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null && _connection.IsOpen)
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.ConnectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}

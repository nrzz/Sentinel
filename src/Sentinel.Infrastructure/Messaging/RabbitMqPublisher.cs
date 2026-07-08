using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Sentinel.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqPublisher(IRabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        var channel = await GetChannelAsync(cancellationToken);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(exchange, routingKey, false, properties, body, cancellationToken);
        _logger.LogDebug("Published message to {Exchange} with routing key {RoutingKey}", exchange, routingKey);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _channel.IsOpen)
        {
            return _channel;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is not null && _channel.IsOpen)
            {
                return _channel;
            }

            var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }
    }
}

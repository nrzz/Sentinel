using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Sentinel.Infrastructure.Messaging;

public interface IRabbitMqTopologyInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class RabbitMqTopologyInitializer : IRabbitMqTopologyInitializer
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    private static readonly string[] Exchanges =
    [
        "sentinel.logs",
        "sentinel.metrics",
        "sentinel.traces",
        "sentinel.alerts",
        "sentinel.notifications"
    ];

    private static readonly (string Queue, string Exchange, string RoutingKey)[] Queues =
    [
        ("collector", "sentinel.logs", "log.received"),
        ("enrichment", "sentinel.logs", "log.received"),
        ("persistence", "sentinel.logs", "log.enriched"),
        ("search-index", "sentinel.logs", "log.enriched"),
        ("ai-analysis", "sentinel.logs", "log.enriched"),
        ("notifications", "sentinel.notifications", "notification.send"),
        ("dead-letter", "sentinel.logs", "dead.letter")
    ];

    public RabbitMqTopologyInitializer(
        IRabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        foreach (var exchange in Exchanges)
        {
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        }

        var dlqArgs = new Dictionary<string, object?> { { "x-dead-letter-exchange", "sentinel.logs" }, { "x-dead-letter-routing-key", "dead.letter" } };

        foreach (var (queue, exchange, routingKey) in Queues)
        {
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                arguments: queue == "dead-letter" ? null : dlqArgs, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("RabbitMQ topology initialized");
    }
}

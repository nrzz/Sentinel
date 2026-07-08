using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Workers;

public sealed class LogEnrichmentWorker : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<LogEnrichmentWorker> _logger;
    private IChannel? _channel;

    public LogEnrichmentWorker(
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqPublisher publisher,
        ILogger<LogEnrichmentWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 50, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                var received = JsonSerializer.Deserialize<LogReceived>(payload);
                if (received is null)
                {
                    await _channel.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
                    return;
                }

                var enriched = Enrich(received);
                await _publisher.PublishAsync(
                    MessagingConstants.LogsExchange,
                    MessagingConstants.LogEnrichedRoutingKey,
                    enriched,
                    stoppingToken);

                await _channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enrich log message");
                await _channel.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(MessagingConstants.EnrichmentQueue, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("Log enrichment worker started on queue {Queue}", MessagingConstants.EnrichmentQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private static LogEnriched Enrich(LogReceived received)
    {
        var normalizedLevel = received.Level.ToUpperInvariant();
        var parsedException = ExtractException(received.Message, received.Attributes);
        var sourceHost = received.Attributes.TryGetValue("host", out var host)
            ? host
            : received.Attributes.TryGetValue("hostname", out var hostname) ? hostname : null;

        return new LogEnriched(
            received.Id,
            received.TenantId,
            received.Timestamp,
            received.Service,
            received.Environment,
            received.Level,
            normalizedLevel,
            received.Message,
            received.Attributes,
            received.TraceId,
            received.SpanId,
            received.CorrelationId,
            parsedException,
            sourceHost,
            received.ReceivedAt,
            DateTimeOffset.UtcNow);
    }

    private static string? ExtractException(string message, IReadOnlyDictionary<string, string> attributes)
    {
        if (attributes.TryGetValue("exception", out var exception))
        {
            return exception;
        }

        if (attributes.TryGetValue("error", out var error))
        {
            return error;
        }

        return message.Contains("Exception", StringComparison.OrdinalIgnoreCase) ? message : null;
    }
}

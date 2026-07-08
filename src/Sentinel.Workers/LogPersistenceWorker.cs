using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Domain.Observability;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Workers;

public sealed class LogPersistenceWorker : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogRepository _logRepository;
    private readonly ILogger<LogPersistenceWorker> _logger;
    private readonly ConcurrentQueue<LogRecord> _rawBatch = new();
    private readonly ConcurrentQueue<EnrichedLogRecord> _enrichedBatch = new();
    private IChannel? _rawChannel;
    private IChannel? _enrichedChannel;

    private const int BatchSize = 500;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    public LogPersistenceWorker(
        IRabbitMqConnectionFactory connectionFactory,
        ILogRepository logRepository,
        ILogger<LogPersistenceWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _logRepository = logRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);

        _rawChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _enrichedChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _rawChannel.BasicQosAsync(0, 100, false, stoppingToken);
        await _enrichedChannel.BasicQosAsync(0, 100, false, stoppingToken);

        var rawConsumer = CreateRawConsumer(stoppingToken);
        var enrichedConsumer = CreateEnrichedConsumer(stoppingToken);

        await _rawChannel.BasicConsumeAsync(MessagingConstants.CollectorQueue, autoAck: false, rawConsumer, stoppingToken);
        await _enrichedChannel.BasicConsumeAsync(MessagingConstants.PersistenceQueue, autoAck: false, enrichedConsumer, stoppingToken);

        _logger.LogInformation("Log persistence worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await FlushBatchesAsync(stoppingToken);
            await Task.Delay(FlushInterval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await FlushBatchesAsync(cancellationToken);

        if (_rawChannel is not null)
        {
            await _rawChannel.CloseAsync(cancellationToken);
            await _rawChannel.DisposeAsync();
        }

        if (_enrichedChannel is not null)
        {
            await _enrichedChannel.CloseAsync(cancellationToken);
            await _enrichedChannel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private AsyncEventingBasicConsumer CreateRawConsumer(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_rawChannel!);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                var received = JsonSerializer.Deserialize<LogReceived>(payload);
                if (received is null)
                {
                    await _rawChannel!.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
                    return;
                }

                _rawBatch.Enqueue(new LogRecord(
                    received.Id,
                    received.TenantId,
                    received.Timestamp,
                    received.Service,
                    received.Environment,
                    received.Level,
                    received.Message,
                    LogRepository.SerializeAttributes(received.Attributes),
                    received.TraceId,
                    received.SpanId,
                    received.CorrelationId));

                await _rawChannel!.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process raw log for persistence");
                await _rawChannel!.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };

        return consumer;
    }

    private AsyncEventingBasicConsumer CreateEnrichedConsumer(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_enrichedChannel!);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                var enriched = JsonSerializer.Deserialize<LogEnriched>(payload);
                if (enriched is null)
                {
                    await _enrichedChannel!.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
                    return;
                }

                _enrichedBatch.Enqueue(new EnrichedLogRecord(
                    enriched.Id,
                    enriched.TenantId,
                    enriched.Timestamp,
                    enriched.Service,
                    enriched.Environment,
                    enriched.Level,
                    enriched.NormalizedLevel,
                    enriched.Message,
                    LogRepository.SerializeAttributes(enriched.Attributes),
                    enriched.TraceId,
                    enriched.SpanId,
                    enriched.CorrelationId,
                    enriched.ParsedException,
                    enriched.SourceHost,
                    enriched.IngestedAt,
                    enriched.EnrichedAt));

                await _enrichedChannel!.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process enriched log for persistence");
                await _enrichedChannel!.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };

        return consumer;
    }

    private async Task FlushBatchesAsync(CancellationToken cancellationToken)
    {
        var rawLogs = DrainBatch(_rawBatch, BatchSize);
        if (rawLogs.Count > 0)
        {
            await _logRepository.BatchInsertRawAsync(rawLogs, cancellationToken);
            _logger.LogDebug("Persisted {Count} raw logs to ClickHouse", rawLogs.Count);
        }

        var enrichedLogs = DrainBatch(_enrichedBatch, BatchSize);
        if (enrichedLogs.Count > 0)
        {
            await _logRepository.BatchInsertEnrichedAsync(enrichedLogs, cancellationToken);
            _logger.LogDebug("Persisted {Count} enriched logs to ClickHouse", enrichedLogs.Count);
        }
    }

    private static List<T> DrainBatch<T>(ConcurrentQueue<T> queue, int maxCount)
    {
        var items = new List<T>(maxCount);
        while (items.Count < maxCount && queue.TryDequeue(out var item))
        {
            items.Add(item);
        }

        return items;
    }
}

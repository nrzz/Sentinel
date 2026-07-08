using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Workers;

public sealed class AlertEvaluationWorker : BackgroundService
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqPublisher _publisher;
    private readonly AlertEvaluationOptions _options;
    private readonly ILogger<AlertEvaluationWorker> _logger;
    private readonly Dictionary<string, int> _errorCounts = new(StringComparer.OrdinalIgnoreCase);
    private IChannel? _metricsChannel;
    private IChannel? _logsChannel;
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

    public AlertEvaluationWorker(
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqPublisher publisher,
        IOptions<AlertEvaluationOptions> options,
        ILogger<AlertEvaluationWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        _metricsChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _logsChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _metricsChannel.QueueDeclareAsync("alert-evaluation-metrics", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _metricsChannel.QueueBindAsync("alert-evaluation-metrics", MessagingConstants.MetricsExchange, MessagingConstants.MetricReceivedRoutingKey, cancellationToken: stoppingToken);

        await _logsChannel.QueueDeclareAsync("alert-evaluation-logs", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _logsChannel.QueueBindAsync("alert-evaluation-logs", MessagingConstants.LogsExchange, MessagingConstants.LogEnrichedRoutingKey, cancellationToken: stoppingToken);

        var metricsConsumer = new AsyncEventingBasicConsumer(_metricsChannel);
        metricsConsumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                var metric = JsonSerializer.Deserialize<MetricReceived>(payload);
                if (metric is not null)
                {
                    await EvaluateMetricAsync(metric, stoppingToken);
                }

                await _metricsChannel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate metric alert");
                await _metricsChannel.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };

        var logsConsumer = new AsyncEventingBasicConsumer(_logsChannel);
        logsConsumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                var log = JsonSerializer.Deserialize<LogEnriched>(payload);
                if (log is not null)
                {
                    await EvaluateLogAsync(log, stoppingToken);
                }

                await _logsChannel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate log alert");
                await _logsChannel.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _metricsChannel.BasicConsumeAsync("alert-evaluation-metrics", autoAck: false, metricsConsumer, stoppingToken);
        await _logsChannel.BasicConsumeAsync("alert-evaluation-logs", autoAck: false, logsConsumer, stoppingToken);

        _logger.LogInformation("Alert evaluation worker started with {RuleCount} metric rules", _options.Rules.Count);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_metricsChannel is not null)
        {
            await _metricsChannel.CloseAsync(cancellationToken);
            await _metricsChannel.DisposeAsync();
        }

        if (_logsChannel is not null)
        {
            await _logsChannel.CloseAsync(cancellationToken);
            await _logsChannel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task EvaluateMetricAsync(MetricReceived metric, CancellationToken cancellationToken)
    {
        foreach (var rule in _options.Rules)
        {
            if (rule.TenantId.HasValue && rule.TenantId.Value != metric.TenantId)
            {
                continue;
            }

            if (!string.Equals(rule.MetricName, metric.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!EvaluateThreshold(metric.Value, rule.Threshold, rule.Operator))
            {
                continue;
            }

            var alert = new AlertTriggered(
                Id: Guid.NewGuid(),
                TenantId: metric.TenantId,
                RuleName: rule.Name,
                Severity: rule.Severity,
                Message: $"Metric {metric.Name} value {metric.Value} breached threshold {rule.Threshold}",
                SourceType: "metric",
                SourceId: metric.Id.ToString(),
                ThresholdValue: rule.Threshold,
                ActualValue: metric.Value,
                TriggeredAt: DateTimeOffset.UtcNow);

            await _publisher.PublishAsync(
                MessagingConstants.AlertsExchange,
                MessagingConstants.AlertTriggeredRoutingKey,
                alert,
                cancellationToken);

            _logger.LogWarning("Alert triggered: {RuleName} for tenant {TenantId}", rule.Name, metric.TenantId);
        }
    }

    private async Task EvaluateLogAsync(LogEnriched log, CancellationToken cancellationToken)
    {
        if (!log.NormalizedLevel.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
            && !log.NormalizedLevel.Equals("FATAL", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ResetWindowIfNeeded();
        var key = $"{log.TenantId}:{log.Service}";
        _errorCounts.TryGetValue(key, out var count);
        _errorCounts[key] = count + 1;

        if (_errorCounts[key] < _options.ErrorLogThresholdPerMinute)
        {
            return;
        }

        var alert = new AlertTriggered(
            Id: Guid.NewGuid(),
            TenantId: log.TenantId,
            RuleName: "error-log-rate",
            Severity: "critical",
            Message: $"Error log rate exceeded {_options.ErrorLogThresholdPerMinute} per minute for service {log.Service}",
            SourceType: "log",
            SourceId: log.Id.ToString(),
            ThresholdValue: _options.ErrorLogThresholdPerMinute,
            ActualValue: _errorCounts[key],
            TriggeredAt: DateTimeOffset.UtcNow);

        await _publisher.PublishAsync(
            MessagingConstants.AlertsExchange,
            MessagingConstants.AlertTriggeredRoutingKey,
            alert,
            cancellationToken);

        _errorCounts[key] = 0;
        _logger.LogWarning("Error rate alert triggered for tenant {TenantId} service {Service}", log.TenantId, log.Service);
    }

    private void ResetWindowIfNeeded()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _windowStart < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _errorCounts.Clear();
        _windowStart = now;
    }

    private static bool EvaluateThreshold(double value, double threshold, string op) =>
        op.ToUpperInvariant() switch
        {
            "LESSTHAN" => value < threshold,
            "EQUALS" => Math.Abs(value - threshold) < double.Epsilon,
            _ => value > threshold
        };
}

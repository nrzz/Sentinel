using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Infrastructure.Health;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;

    public RabbitMqHealthCheck(IRabbitMqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is closed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable", ex);
        }
    }
}

using ClickHouse.Client.ADO;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Infrastructure.Health;

public sealed class ClickHouseHealthCheck : IHealthCheck
{
    private readonly IClickHouseConnectionFactory _connectionFactory;

    public ClickHouseHealthCheck(IClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("ClickHouse is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClickHouse is unreachable", ex);
        }
    }
}

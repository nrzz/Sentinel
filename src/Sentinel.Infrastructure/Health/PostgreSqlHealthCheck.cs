using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Health;

public sealed class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public PostgreSqlHealthCheck(IPostgreSqlConnectionFactory connectionFactory)
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
            return HealthCheckResult.Healthy("PostgreSQL is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable", ex);
        }
    }
}

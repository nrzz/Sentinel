using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Infrastructure.Persistence.Redis;

namespace Sentinel.Infrastructure.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisConnectionFactory _connectionFactory;

    public RedisHealthCheck(IRedisConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionFactory.GetConnectionAsync();
            var db = connection.GetDatabase();
            var pong = await db.PingAsync();
            return HealthCheckResult.Healthy($"Redis is reachable (ping: {pong.TotalMilliseconds:F1}ms)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}

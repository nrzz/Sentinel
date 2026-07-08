using global::ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public interface IClickHouseConnectionFactory
{
    Task<ClickHouseConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class ClickHouseConnectionFactory : IClickHouseConnectionFactory
{
    private readonly ClickHouseOptions _options;

    public ClickHouseConnectionFactory(IOptions<ClickHouseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ClickHouseConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new ClickHouseConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

using Microsoft.Extensions.Options;
using Npgsql;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

public interface IPostgreSqlConnectionFactory
{
    Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class PostgreSqlConnectionFactory : IPostgreSqlConnectionFactory
{
    private readonly PostgreSqlOptions _options;

    public PostgreSqlConnectionFactory(IOptions<PostgreSqlOptions> options)
    {
        _options = options.Value;
    }

    public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

using global::ClickHouse.Client.ADO;
using global::ClickHouse.Client.Copy;
using Microsoft.Extensions.Logging;
using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed class MetricRepository : IMetricRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;
    private readonly ILogger<MetricRepository> _logger;

    private static readonly string[] Columns =
    [
        "id", "tenant_id", "timestamp", "name", "value", "unit", "tags", "service", "environment"
    ];

    public MetricRepository(IClickHouseConnectionFactory connectionFactory, ILogger<MetricRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BatchInsertAsync(IReadOnlyList<MetricRecord> metrics, CancellationToken cancellationToken = default)
    {
        if (metrics.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = metrics.Select(metric => new object?[]
        {
            metric.Id,
            metric.TenantId,
            metric.Timestamp.UtcDateTime,
            metric.Name,
            metric.Value,
            metric.Unit,
            metric.TagsJson,
            metric.Service,
            metric.Environment
        }).ToList();

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "metrics",
            ColumnNames = Columns,
            BatchSize = 10_000
        };

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} metric records", metrics.Count);
    }

    public async Task<IReadOnlyList<MetricRecord>> QueryAsync(MetricQueryFilters filters, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var conditions = new List<string> { "tenant_id = {tenant_id:UUID}" };
        var parameters = new Dictionary<string, object?> { ["tenant_id"] = filters.TenantId };

        if (filters.From.HasValue)
        {
            conditions.Add("timestamp >= {from:DateTime64(3)}");
            parameters["from"] = filters.From.Value.UtcDateTime;
        }

        if (filters.To.HasValue)
        {
            conditions.Add("timestamp <= {to:DateTime64(3)}");
            parameters["to"] = filters.To.Value.UtcDateTime;
        }

        if (!string.IsNullOrWhiteSpace(filters.Name))
        {
            conditions.Add("name = {name:String}");
            parameters["name"] = filters.Name;
        }

        if (!string.IsNullOrWhiteSpace(filters.Service))
        {
            conditions.Add("service = {service:String}");
            parameters["service"] = filters.Service;
        }

        if (!string.IsNullOrWhiteSpace(filters.Environment))
        {
            conditions.Add("environment = {environment:String}");
            parameters["environment"] = filters.Environment;
        }

        parameters["limit"] = (uint)filters.Limit;

        var sql =
            "SELECT id, tenant_id, timestamp, name, value, unit, tags, service, environment " +
            "FROM metrics WHERE " + string.Join(" AND ", conditions) +
            " ORDER BY timestamp DESC LIMIT {limit:UInt32}";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new global::ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
            {
                ParameterName = name,
                Value = value
            });
        }

        var results = new List<MetricRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MetricRecord(
                Id: reader.GetGuid(0),
                TenantId: reader.GetGuid(1),
                Timestamp: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)),
                Name: reader.GetString(3),
                Value: reader.GetDouble(4),
                Unit: reader.GetString(5),
                TagsJson: reader.GetString(6),
                Service: reader.GetString(7),
                Environment: reader.GetString(8)));
        }

        return results;
    }
}

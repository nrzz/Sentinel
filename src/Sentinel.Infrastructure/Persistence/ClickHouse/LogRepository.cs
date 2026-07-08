using System.Globalization;
using System.Text.Json;
using global::ClickHouse.Client.ADO;
using global::ClickHouse.Client.Copy;
using Microsoft.Extensions.Logging;
using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed class LogRepository : ILogRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;
    private readonly ILogger<LogRepository> _logger;

    private static readonly string[] RawColumns =
    [
        "id", "tenant_id", "timestamp", "service", "environment", "level",
        "message", "attributes", "trace_id", "span_id", "correlation_id", "ingested_at"
    ];

    private static readonly string[] EnrichedColumns =
    [
        "id", "tenant_id", "timestamp", "service", "environment", "level", "normalized_level",
        "message", "attributes", "trace_id", "span_id", "correlation_id",
        "parsed_exception", "source_host", "ingested_at", "enriched_at"
    ];

    public LogRepository(IClickHouseConnectionFactory connectionFactory, ILogger<LogRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BatchInsertRawAsync(IReadOnlyList<LogRecord> logs, CancellationToken cancellationToken = default)
    {
        if (logs.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = logs.Select(log => new object?[]
        {
            log.Id,
            log.TenantId,
            log.Timestamp.UtcDateTime,
            log.Service,
            log.Environment,
            log.Level,
            log.Message,
            log.AttributesJson,
            log.TraceId ?? string.Empty,
            log.SpanId ?? string.Empty,
            log.CorrelationId ?? string.Empty,
            DateTime.UtcNow
        }).ToList();

        await BulkInsertAsync(connection, "logs_raw", RawColumns, rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} raw log records", logs.Count);
    }

    public async Task BatchInsertEnrichedAsync(IReadOnlyList<EnrichedLogRecord> logs, CancellationToken cancellationToken = default)
    {
        if (logs.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = logs.Select(log => new object?[]
        {
            log.Id,
            log.TenantId,
            log.Timestamp.UtcDateTime,
            log.Service,
            log.Environment,
            log.Level,
            log.NormalizedLevel,
            log.Message,
            log.AttributesJson,
            log.TraceId ?? string.Empty,
            log.SpanId ?? string.Empty,
            log.CorrelationId ?? string.Empty,
            log.ParsedException ?? string.Empty,
            log.SourceHost ?? string.Empty,
            log.IngestedAt.UtcDateTime,
            log.EnrichedAt.UtcDateTime
        }).ToList();

        await BulkInsertAsync(connection, "logs_enriched", EnrichedColumns, rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} enriched log records", logs.Count);
    }

    public async Task<LogSearchResult> SearchAsync(LogSearchFilters filters, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrWhiteSpace(filters.Level))
        {
            conditions.Add("normalized_level = {level:String}");
            parameters["level"] = filters.Level;
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

        if (!string.IsNullOrWhiteSpace(filters.TraceId))
        {
            conditions.Add("trace_id = {trace_id:String}");
            parameters["trace_id"] = filters.TraceId;
        }

        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            conditions.Add("positionCaseInsensitive(message, {query:String}) > 0");
            parameters["query"] = filters.Query;
        }

        var whereClause = string.Join(" AND ", conditions);
        var countSql = $"SELECT count() FROM logs_enriched WHERE {whereClause}";
        var totalCount = await ExecuteScalarAsync<long>(connection, countSql, parameters, cancellationToken);

        var searchSql =
            "SELECT id, tenant_id, timestamp, service, environment, level, normalized_level, " +
            "message, attributes, trace_id, span_id, correlation_id, " +
            "parsed_exception, source_host, ingested_at, enriched_at " +
            "FROM logs_enriched WHERE " + whereClause +
            " ORDER BY timestamp DESC LIMIT {limit:UInt32} OFFSET {offset:UInt32}";

        parameters["limit"] = (uint)filters.Limit;
        parameters["offset"] = (uint)filters.Offset;

        var items = await ExecuteQueryAsync(connection, searchSql, parameters, cancellationToken);
        return new LogSearchResult(items, totalCount);
    }

    private static async Task BulkInsertAsync(
        ClickHouseConnection connection,
        string tableName,
        string[] columns,
        List<object?[]> rows,
        CancellationToken cancellationToken)
    {
        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = tableName,
            ColumnNames = columns,
            BatchSize = 10_000
        };

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, cancellationToken);
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        ClickHouseConnection connection,
        string sql,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindParameters(command, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (T)Convert.ChangeType(result!, typeof(T), CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<EnrichedLogRecord>> ExecuteQueryAsync(
        ClickHouseConnection connection,
        string sql,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindParameters(command, parameters);

        var results = new List<EnrichedLogRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new EnrichedLogRecord(
                Id: reader.GetGuid(0),
                TenantId: reader.GetGuid(1),
                Timestamp: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)),
                Service: reader.GetString(3),
                Environment: reader.GetString(4),
                Level: reader.GetString(5),
                NormalizedLevel: reader.GetString(6),
                Message: reader.GetString(7),
                AttributesJson: reader.GetString(8),
                TraceId: NullIfEmpty(reader.GetString(9)),
                SpanId: NullIfEmpty(reader.GetString(10)),
                CorrelationId: NullIfEmpty(reader.GetString(11)),
                ParsedException: NullIfEmpty(reader.GetString(12)),
                SourceHost: NullIfEmpty(reader.GetString(13)),
                IngestedAt: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc)),
                EnrichedAt: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(15), DateTimeKind.Utc))));
        }

        return results;
    }

    private static void BindParameters(global::ClickHouse.Client.ADO.ClickHouseCommand command, Dictionary<string, object?> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new global::ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
            {
                ParameterName = name,
                Value = value
            });
        }
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

    public static string SerializeAttributes(IReadOnlyDictionary<string, string> attributes) =>
        JsonSerializer.Serialize(attributes);
}

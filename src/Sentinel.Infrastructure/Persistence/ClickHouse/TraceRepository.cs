using global::ClickHouse.Client.ADO;
using global::ClickHouse.Client.Copy;
using Microsoft.Extensions.Logging;
using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed class TraceRepository : ITraceRepository
{
    private readonly IClickHouseConnectionFactory _connectionFactory;
    private readonly ILogger<TraceRepository> _logger;

    private static readonly string[] TraceColumns =
    [
        "id", "tenant_id", "trace_id", "timestamp", "service", "name", "duration_ms", "status", "attributes"
    ];

    private static readonly string[] SpanColumns =
    [
        "id", "tenant_id", "trace_id", "span_id", "parent_span_id", "timestamp",
        "name", "service", "duration_ms", "status", "attributes"
    ];

    private static readonly string[] DeploymentColumns =
    [
        "id", "tenant_id", "timestamp", "service", "version", "environment",
        "status", "commit_sha", "repository", "metadata"
    ];

    public TraceRepository(IClickHouseConnectionFactory connectionFactory, ILogger<TraceRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task BatchInsertTracesAsync(IReadOnlyList<TraceRecord> traces, CancellationToken cancellationToken = default)
    {
        if (traces.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = traces.Select(trace => new object?[]
        {
            trace.Id,
            trace.TenantId,
            trace.TraceId,
            trace.Timestamp.UtcDateTime,
            trace.Service,
            trace.Name,
            trace.DurationMs,
            trace.Status,
            trace.AttributesJson
        }).ToList();

        await BulkInsertAsync(connection, "traces", TraceColumns, rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} trace records", traces.Count);
    }

    public async Task BatchInsertSpansAsync(IReadOnlyList<TraceSpanRecord> spans, CancellationToken cancellationToken = default)
    {
        if (spans.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = spans.Select(span => new object?[]
        {
            span.Id,
            span.TenantId,
            span.TraceId,
            span.SpanId,
            span.ParentSpanId ?? string.Empty,
            span.Timestamp.UtcDateTime,
            span.Name,
            span.Service,
            span.DurationMs,
            span.Status,
            span.AttributesJson
        }).ToList();

        await BulkInsertAsync(connection, "trace_spans", SpanColumns, rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} trace span records", spans.Count);
    }

    public async Task BatchInsertDeploymentsAsync(IReadOnlyList<DeploymentRecord> deployments, CancellationToken cancellationToken = default)
    {
        if (deployments.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = deployments.Select(deployment => new object?[]
        {
            deployment.Id,
            deployment.TenantId,
            deployment.Timestamp.UtcDateTime,
            deployment.Service,
            deployment.Version,
            deployment.Environment,
            deployment.Status,
            deployment.CommitSha,
            deployment.Repository,
            deployment.MetadataJson
        }).ToList();

        await BulkInsertAsync(connection, "deployments", DeploymentColumns, rows, cancellationToken);
        _logger.LogDebug("Inserted {Count} deployment records", deployments.Count);
    }

    public async Task<IReadOnlyList<TraceRecord>> QueryTracesAsync(TraceQueryFilters filters, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrWhiteSpace(filters.Service))
        {
            conditions.Add("service = {service:String}");
            parameters["service"] = filters.Service;
        }

        if (!string.IsNullOrWhiteSpace(filters.TraceId))
        {
            conditions.Add("trace_id = {trace_id:String}");
            parameters["trace_id"] = filters.TraceId;
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            conditions.Add("status = {status:String}");
            parameters["status"] = filters.Status;
        }

        parameters["limit"] = (uint)filters.Limit;

        var sql =
            "SELECT id, tenant_id, trace_id, timestamp, service, name, duration_ms, status, attributes " +
            "FROM traces WHERE " + string.Join(" AND ", conditions) +
            " ORDER BY timestamp DESC LIMIT {limit:UInt32}";

        return await QueryTracesInternalAsync(connection, sql, parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<TraceSpanRecord>> GetSpansByTraceIdAsync(
        Guid tenantId,
        string traceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT id, tenant_id, trace_id, span_id, parent_span_id, timestamp,
                   name, service, duration_ms, status, attributes
            FROM trace_spans
            WHERE tenant_id = {tenant_id:UUID} AND trace_id = {trace_id:String}
            ORDER BY timestamp ASC
            """;

        var parameters = new Dictionary<string, object?>
        {
            ["tenant_id"] = tenantId,
            ["trace_id"] = traceId
        };

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

        var results = new List<TraceSpanRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var parentSpanId = reader.GetString(4);
            results.Add(new TraceSpanRecord(
                Id: reader.GetGuid(0),
                TenantId: reader.GetGuid(1),
                TraceId: reader.GetString(2),
                SpanId: reader.GetString(3),
                ParentSpanId: string.IsNullOrEmpty(parentSpanId) ? null : parentSpanId,
                Timestamp: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
                Name: reader.GetString(6),
                Service: reader.GetString(7),
                DurationMs: reader.GetDouble(8),
                Status: reader.GetString(9),
                AttributesJson: reader.GetString(10)));
        }

        return results;
    }

    private static async Task<IReadOnlyList<TraceRecord>> QueryTracesInternalAsync(
        ClickHouseConnection connection,
        string sql,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
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

        var results = new List<TraceRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TraceRecord(
                Id: reader.GetGuid(0),
                TenantId: reader.GetGuid(1),
                TraceId: reader.GetString(2),
                Timestamp: new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc)),
                Service: reader.GetString(4),
                Name: reader.GetString(5),
                DurationMs: reader.GetDouble(6),
                Status: reader.GetString(7),
                AttributesJson: reader.GetString(8)));
        }

        return results;
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
}

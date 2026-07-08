using global::ClickHouse.Client.ADO;
using Microsoft.Extensions.Logging;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed class ClickHouseMigrator : IClickHouseMigrator
{
    private readonly IClickHouseConnectionFactory _connectionFactory;
    private readonly ILogger<ClickHouseMigrator> _logger;

    private static readonly string[] TableDefinitions =
    [
        """
        CREATE TABLE IF NOT EXISTS logs_raw (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            service LowCardinality(String),
            environment LowCardinality(String),
            level LowCardinality(String),
            message String,
            attributes String,
            trace_id String,
            span_id String,
            correlation_id String,
            ingested_at DateTime64(3, 'UTC') DEFAULT now64(3)
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS logs_enriched (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            service LowCardinality(String),
            environment LowCardinality(String),
            level LowCardinality(String),
            normalized_level LowCardinality(String),
            message String,
            attributes String,
            trace_id String,
            span_id String,
            correlation_id String,
            parsed_exception String,
            source_host String,
            ingested_at DateTime64(3, 'UTC'),
            enriched_at DateTime64(3, 'UTC')
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS metrics (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            name LowCardinality(String),
            value Float64,
            unit LowCardinality(String),
            tags String,
            service LowCardinality(String),
            environment LowCardinality(String)
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS traces (
            id UUID,
            tenant_id UUID,
            trace_id String,
            timestamp DateTime64(3, 'UTC'),
            service LowCardinality(String),
            name String,
            duration_ms Float64,
            status LowCardinality(String),
            attributes String
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS trace_spans (
            id UUID,
            tenant_id UUID,
            trace_id String,
            span_id String,
            parent_span_id String,
            timestamp DateTime64(3, 'UTC'),
            name String,
            service LowCardinality(String),
            duration_ms Float64,
            status LowCardinality(String),
            attributes String
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS deployments (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            service LowCardinality(String),
            version String,
            environment LowCardinality(String),
            status LowCardinality(String),
            commit_sha String,
            repository String,
            metadata String
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS correlations (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            correlation_type LowCardinality(String),
            related_ids Array(String),
            confidence Float64,
            description String
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """,
        """
        CREATE TABLE IF NOT EXISTS anomalies (
            id UUID,
            tenant_id UUID,
            timestamp DateTime64(3, 'UTC'),
            anomaly_type LowCardinality(String),
            source_type LowCardinality(String),
            source_id String,
            score Float64,
            description String
        ) ENGINE = MergeTree()
        PARTITION BY toYYYYMM(timestamp)
        ORDER BY (tenant_id, timestamp)
        """
    ];

    public ClickHouseMigrator(IClickHouseConnectionFactory connectionFactory, ILogger<ClickHouseMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        foreach (var ddl in TableDefinitions)
        {
            await ExecuteAsync(connection, ddl, cancellationToken);
        }

        _logger.LogInformation("ClickHouse schema migration completed");
    }

    private static async Task ExecuteAsync(ClickHouseConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

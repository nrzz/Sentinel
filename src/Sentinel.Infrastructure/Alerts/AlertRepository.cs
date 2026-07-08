using System.Text.Json;
using Npgsql;
using Sentinel.Domain.Alerts;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Alerts;

public sealed class AlertRepository : IAlertRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public AlertRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AlertRule?> GetRuleByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, description, query, condition, severity, status,
                   evaluation_interval_seconds, notification_channels, created_by, created_at, updated_at
            FROM alert_rules
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRule(reader) : null;
    }

    public async Task<IReadOnlyList<AlertRule>> ListRulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, description, query, condition, severity, status,
                   evaluation_interval_seconds, notification_channels, created_by, created_at, updated_at
            FROM alert_rules
            WHERE tenant_id = @tenant_id
            ORDER BY created_at DESC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);

        var rules = new List<AlertRule>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rules.Add(MapRule(reader));
        }

        return rules;
    }

    public async Task CreateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO alert_rules (
                id, tenant_id, name, description, query, condition, severity, status,
                evaluation_interval_seconds, notification_channels, created_by, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @name, @description, @query, @condition, @severity, @status,
                @evaluation_interval_seconds, @notification_channels::jsonb, @created_by, @created_at, @updated_at)
            """,
            connection);

        AddRuleParameters(command, rule);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE alert_rules SET
                name = @name,
                description = @description,
                query = @query,
                condition = @condition,
                severity = @severity,
                status = @status,
                evaluation_interval_seconds = @evaluation_interval_seconds,
                notification_channels = @notification_channels::jsonb,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        AddRuleParameters(command, rule);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteRuleAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "DELETE FROM alert_rules WHERE tenant_id = @tenant_id AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AlertExecution?> GetExecutionByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, alert_rule_id, status, severity, message, matched_value,
                   triggered_at, resolved_at, correlation_id, created_at, updated_at
            FROM alert_executions
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapExecution(reader) : null;
    }

    public async Task<IReadOnlyList<AlertExecution>> ListExecutionsByRuleAsync(
        Guid tenantId,
        Guid alertRuleId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, alert_rule_id, status, severity, message, matched_value,
                   triggered_at, resolved_at, correlation_id, created_at, updated_at
            FROM alert_executions
            WHERE tenant_id = @tenant_id AND alert_rule_id = @alert_rule_id
            ORDER BY triggered_at DESC
            LIMIT @limit
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("alert_rule_id", alertRuleId);
        command.Parameters.AddWithValue("limit", limit);

        var executions = new List<AlertExecution>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            executions.Add(MapExecution(reader));
        }

        return executions;
    }

    public async Task CreateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO alert_executions (
                id, tenant_id, alert_rule_id, status, severity, message, matched_value,
                triggered_at, resolved_at, correlation_id, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @alert_rule_id, @status, @severity, @message, @matched_value,
                @triggered_at, @resolved_at, @correlation_id, @created_at, @updated_at)
            """,
            connection);

        command.Parameters.AddWithValue("id", execution.Id);
        command.Parameters.AddWithValue("tenant_id", execution.TenantId);
        command.Parameters.AddWithValue("alert_rule_id", execution.AlertRuleId);
        command.Parameters.AddWithValue("status", (int)execution.Status);
        command.Parameters.AddWithValue("severity", (int)execution.Severity);
        command.Parameters.AddWithValue("message", execution.Message);
        command.Parameters.AddWithValue("matched_value", (object?)execution.MatchedValue ?? DBNull.Value);
        command.Parameters.AddWithValue("triggered_at", execution.TriggeredAt);
        command.Parameters.AddWithValue("resolved_at", (object?)execution.ResolvedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("correlation_id", (object?)execution.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", execution.CreatedAt);
        command.Parameters.AddWithValue("updated_at", execution.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE alert_executions SET
                status = @status,
                message = @message,
                resolved_at = @resolved_at,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        command.Parameters.AddWithValue("id", execution.Id);
        command.Parameters.AddWithValue("tenant_id", execution.TenantId);
        command.Parameters.AddWithValue("status", (int)execution.Status);
        command.Parameters.AddWithValue("message", execution.Message);
        command.Parameters.AddWithValue("resolved_at", (object?)execution.ResolvedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_at", execution.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CreateNotificationAsync(AlertNotification notification, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO alert_notifications (
                id, tenant_id, alert_execution_id, channel, recipient, subject, body,
                status, sent_at, error_message, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @alert_execution_id, @channel, @recipient, @subject, @body,
                @status, @sent_at, @error_message, @created_at, @updated_at)
            """,
            connection);

        command.Parameters.AddWithValue("id", notification.Id);
        command.Parameters.AddWithValue("tenant_id", notification.TenantId);
        command.Parameters.AddWithValue("alert_execution_id", notification.AlertExecutionId);
        command.Parameters.AddWithValue("channel", notification.Channel);
        command.Parameters.AddWithValue("recipient", notification.Recipient);
        command.Parameters.AddWithValue("subject", notification.Subject);
        command.Parameters.AddWithValue("body", notification.Body);
        command.Parameters.AddWithValue("status", (int)notification.Status);
        command.Parameters.AddWithValue("sent_at", (object?)notification.SentAt ?? DBNull.Value);
        command.Parameters.AddWithValue("error_message", (object?)notification.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", notification.CreatedAt);
        command.Parameters.AddWithValue("updated_at", notification.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddRuleParameters(NpgsqlCommand command, AlertRule rule)
    {
        command.Parameters.AddWithValue("id", rule.Id);
        command.Parameters.AddWithValue("tenant_id", rule.TenantId);
        command.Parameters.AddWithValue("name", rule.Name);
        command.Parameters.AddWithValue("description", rule.Description);
        command.Parameters.AddWithValue("query", rule.Query);
        command.Parameters.AddWithValue("condition", rule.Condition);
        command.Parameters.AddWithValue("severity", (int)rule.Severity);
        command.Parameters.AddWithValue("status", (int)rule.Status);
        command.Parameters.AddWithValue("evaluation_interval_seconds", (long)rule.EvaluationInterval.TotalSeconds);
        command.Parameters.AddWithValue("notification_channels", JsonSerializer.Serialize(rule.NotificationChannels));
        command.Parameters.AddWithValue("created_by", (object?)rule.CreatedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", rule.CreatedAt);
        command.Parameters.AddWithValue("updated_at", rule.UpdatedAt);
    }

    private static AlertRule MapRule(NpgsqlDataReader reader)
    {
        var channelsJson = reader.GetString(reader.GetOrdinal("notification_channels"));
        var channels = JsonSerializer.Deserialize<List<string>>(channelsJson) ?? [];

        return AlertRule.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetString(reader.GetOrdinal("description")),
            reader.GetString(reader.GetOrdinal("query")),
            reader.GetString(reader.GetOrdinal("condition")),
            (AlertSeverity)reader.GetInt32(reader.GetOrdinal("severity")),
            (AlertRuleStatus)reader.GetInt32(reader.GetOrdinal("status")),
            TimeSpan.FromSeconds(reader.GetInt64(reader.GetOrdinal("evaluation_interval_seconds"))),
            channels,
            reader.IsDBNull(reader.GetOrdinal("created_by"))
                ? null
                : reader.GetString(reader.GetOrdinal("created_by")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
    }

    private static AlertExecution MapExecution(NpgsqlDataReader reader)
    {
        return AlertExecution.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetGuid(reader.GetOrdinal("alert_rule_id")),
            (AlertExecutionStatus)reader.GetInt32(reader.GetOrdinal("status")),
            (AlertSeverity)reader.GetInt32(reader.GetOrdinal("severity")),
            reader.GetString(reader.GetOrdinal("message")),
            reader.IsDBNull(reader.GetOrdinal("matched_value"))
                ? null
                : reader.GetString(reader.GetOrdinal("matched_value")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("triggered_at")),
            reader.IsDBNull(reader.GetOrdinal("resolved_at"))
                ? null
                : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("resolved_at")),
            reader.IsDBNull(reader.GetOrdinal("correlation_id"))
                ? null
                : reader.GetString(reader.GetOrdinal("correlation_id")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
    }
}

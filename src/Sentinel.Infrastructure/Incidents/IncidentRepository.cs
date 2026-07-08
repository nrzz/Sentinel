using Npgsql;
using Sentinel.Domain.Incidents;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Incidents;

public sealed class IncidentRepository : IIncidentRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public IncidentRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Incident?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, title, description, severity, status, assigned_to,
                   source_alert_execution_id, created_by, resolved_at, created_at, updated_at
            FROM incidents
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapIncident(reader) : null;
    }

    public async Task<IReadOnlyList<Incident>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, title, description, severity, status, assigned_to,
                   source_alert_execution_id, created_by, resolved_at, created_at, updated_at
            FROM incidents
            WHERE tenant_id = @tenant_id
            ORDER BY created_at DESC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);

        var incidents = new List<Incident>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            incidents.Add(MapIncident(reader));
        }

        return incidents;
    }

    public async Task CreateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO incidents (
                id, tenant_id, title, description, severity, status, assigned_to,
                source_alert_execution_id, created_by, resolved_at, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @title, @description, @severity, @status, @assigned_to,
                @source_alert_execution_id, @created_by, @resolved_at, @created_at, @updated_at)
            """,
            connection);

        AddIncidentParameters(command, incident);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE incidents SET
                title = @title,
                description = @description,
                severity = @severity,
                status = @status,
                assigned_to = @assigned_to,
                resolved_at = @resolved_at,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        AddIncidentParameters(command, incident);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "DELETE FROM incidents WHERE tenant_id = @tenant_id AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentTimelineEntry>> ListTimelineAsync(
        Guid tenantId,
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, incident_id, entry_type, message, actor, metadata_json, created_at, updated_at
            FROM incident_timeline_entries
            WHERE tenant_id = @tenant_id AND incident_id = @incident_id
            ORDER BY created_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("incident_id", incidentId);

        var entries = new List<IncidentTimelineEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(MapTimelineEntry(reader));
        }

        return entries;
    }

    public async Task AddTimelineEntryAsync(IncidentTimelineEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO incident_timeline_entries (
                id, tenant_id, incident_id, entry_type, message, actor, metadata_json, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @incident_id, @entry_type, @message, @actor, @metadata_json::jsonb, @created_at, @updated_at)
            """,
            connection);

        command.Parameters.AddWithValue("id", entry.Id);
        command.Parameters.AddWithValue("tenant_id", entry.TenantId);
        command.Parameters.AddWithValue("incident_id", entry.IncidentId);
        command.Parameters.AddWithValue("entry_type", (int)entry.EntryType);
        command.Parameters.AddWithValue("message", entry.Message);
        command.Parameters.AddWithValue("actor", (object?)entry.Actor ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata_json", (object?)entry.MetadataJson ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", entry.CreatedAt);
        command.Parameters.AddWithValue("updated_at", entry.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentComment>> ListCommentsAsync(
        Guid tenantId,
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, incident_id, author, content, is_internal, created_at, updated_at
            FROM incident_comments
            WHERE tenant_id = @tenant_id AND incident_id = @incident_id
            ORDER BY created_at ASC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("incident_id", incidentId);

        var comments = new List<IncidentComment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            comments.Add(MapComment(reader));
        }

        return comments;
    }

    public async Task<IncidentComment?> GetCommentByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, incident_id, author, content, is_internal, created_at, updated_at
            FROM incident_comments
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapComment(reader) : null;
    }

    public async Task AddCommentAsync(IncidentComment comment, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO incident_comments (
                id, tenant_id, incident_id, author, content, is_internal, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @incident_id, @author, @content, @is_internal, @created_at, @updated_at)
            """,
            connection);

        command.Parameters.AddWithValue("id", comment.Id);
        command.Parameters.AddWithValue("tenant_id", comment.TenantId);
        command.Parameters.AddWithValue("incident_id", comment.IncidentId);
        command.Parameters.AddWithValue("author", comment.Author);
        command.Parameters.AddWithValue("content", comment.Content);
        command.Parameters.AddWithValue("is_internal", comment.IsInternal);
        command.Parameters.AddWithValue("created_at", comment.CreatedAt);
        command.Parameters.AddWithValue("updated_at", comment.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateCommentAsync(IncidentComment comment, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE incident_comments SET
                content = @content,
                is_internal = @is_internal,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        command.Parameters.AddWithValue("id", comment.Id);
        command.Parameters.AddWithValue("tenant_id", comment.TenantId);
        command.Parameters.AddWithValue("content", comment.Content);
        command.Parameters.AddWithValue("is_internal", comment.IsInternal);
        command.Parameters.AddWithValue("updated_at", comment.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteCommentAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "DELETE FROM incident_comments WHERE tenant_id = @tenant_id AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddIncidentParameters(NpgsqlCommand command, Incident incident)
    {
        command.Parameters.AddWithValue("id", incident.Id);
        command.Parameters.AddWithValue("tenant_id", incident.TenantId);
        command.Parameters.AddWithValue("title", incident.Title);
        command.Parameters.AddWithValue("description", incident.Description);
        command.Parameters.AddWithValue("severity", (int)incident.Severity);
        command.Parameters.AddWithValue("status", (int)incident.Status);
        command.Parameters.AddWithValue("assigned_to", (object?)incident.AssignedTo ?? DBNull.Value);
        command.Parameters.AddWithValue("source_alert_execution_id", (object?)incident.SourceAlertExecutionId ?? DBNull.Value);
        command.Parameters.AddWithValue("created_by", (object?)incident.CreatedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("resolved_at", (object?)incident.ResolvedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", incident.CreatedAt);
        command.Parameters.AddWithValue("updated_at", incident.UpdatedAt);
    }

    private static Incident MapIncident(NpgsqlDataReader reader) =>
        Incident.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetString(reader.GetOrdinal("title")),
            reader.GetString(reader.GetOrdinal("description")),
            (IncidentSeverity)reader.GetInt32(reader.GetOrdinal("severity")),
            (IncidentStatus)reader.GetInt32(reader.GetOrdinal("status")),
            reader.IsDBNull(reader.GetOrdinal("assigned_to"))
                ? null
                : reader.GetString(reader.GetOrdinal("assigned_to")),
            reader.IsDBNull(reader.GetOrdinal("source_alert_execution_id"))
                ? null
                : reader.GetGuid(reader.GetOrdinal("source_alert_execution_id")),
            reader.IsDBNull(reader.GetOrdinal("created_by"))
                ? null
                : reader.GetString(reader.GetOrdinal("created_by")),
            reader.IsDBNull(reader.GetOrdinal("resolved_at"))
                ? null
                : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("resolved_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));

    private static IncidentTimelineEntry MapTimelineEntry(NpgsqlDataReader reader) =>
        IncidentTimelineEntry.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetGuid(reader.GetOrdinal("incident_id")),
            (IncidentTimelineEntryType)reader.GetInt32(reader.GetOrdinal("entry_type")),
            reader.GetString(reader.GetOrdinal("message")),
            reader.IsDBNull(reader.GetOrdinal("actor"))
                ? null
                : reader.GetString(reader.GetOrdinal("actor")),
            reader.IsDBNull(reader.GetOrdinal("metadata_json"))
                ? null
                : reader.GetString(reader.GetOrdinal("metadata_json")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));

    private static IncidentComment MapComment(NpgsqlDataReader reader) =>
        IncidentComment.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetGuid(reader.GetOrdinal("incident_id")),
            reader.GetString(reader.GetOrdinal("author")),
            reader.GetString(reader.GetOrdinal("content")),
            reader.GetBoolean(reader.GetOrdinal("is_internal")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
}

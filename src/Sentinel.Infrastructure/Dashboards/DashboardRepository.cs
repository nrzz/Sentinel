using Npgsql;
using Sentinel.Domain.Dashboards;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Dashboards;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public DashboardRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Dashboard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, description, layout_json, is_default, created_by, created_at, updated_at
            FROM dashboards
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapDashboard(reader) : null;
    }

    public async Task<IReadOnlyList<Dashboard>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, description, layout_json, is_default, created_by, created_at, updated_at
            FROM dashboards
            WHERE tenant_id = @tenant_id
            ORDER BY is_default DESC, name ASC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);

        var dashboards = new List<Dashboard>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            dashboards.Add(MapDashboard(reader));
        }

        return dashboards;
    }

    public async Task CreateAsync(Dashboard dashboard, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO dashboards (
                id, tenant_id, name, description, layout_json, is_default, created_by, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @name, @description, @layout_json::jsonb, @is_default, @created_by, @created_at, @updated_at)
            """,
            connection);

        AddParameters(command, dashboard);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Dashboard dashboard, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE dashboards SET
                name = @name,
                description = @description,
                layout_json = @layout_json::jsonb,
                is_default = @is_default,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        AddParameters(command, dashboard);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "DELETE FROM dashboards WHERE tenant_id = @tenant_id AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, Dashboard dashboard)
    {
        command.Parameters.AddWithValue("id", dashboard.Id);
        command.Parameters.AddWithValue("tenant_id", dashboard.TenantId);
        command.Parameters.AddWithValue("name", dashboard.Name);
        command.Parameters.AddWithValue("description", dashboard.Description);
        command.Parameters.AddWithValue("layout_json", dashboard.LayoutJson);
        command.Parameters.AddWithValue("is_default", dashboard.IsDefault);
        command.Parameters.AddWithValue("created_by", (object?)dashboard.CreatedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", dashboard.CreatedAt);
        command.Parameters.AddWithValue("updated_at", dashboard.UpdatedAt);
    }

    private static Dashboard MapDashboard(NpgsqlDataReader reader) =>
        Dashboard.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetString(reader.GetOrdinal("description")),
            reader.GetString(reader.GetOrdinal("layout_json")),
            reader.GetBoolean(reader.GetOrdinal("is_default")),
            reader.IsDBNull(reader.GetOrdinal("created_by"))
                ? null
                : reader.GetString(reader.GetOrdinal("created_by")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
}

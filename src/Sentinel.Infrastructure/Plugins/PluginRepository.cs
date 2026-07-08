using Npgsql;
using Sentinel.Domain.Plugins;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.Plugins;

public sealed class PluginRepository : IPluginRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public PluginRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Plugin?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, version, description, assembly_name, configuration_json,
                   status, installed_by, installed_at, created_at, updated_at
            FROM plugins
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPlugin(reader) : null;
    }

    public async Task<Plugin?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, version, description, assembly_name, configuration_json,
                   status, installed_by, installed_at, created_at, updated_at
            FROM plugins
            WHERE tenant_id = @tenant_id AND name = @name
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("name", name);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapPlugin(reader) : null;
    }

    public async Task<IReadOnlyList<Plugin>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, name, version, description, assembly_name, configuration_json,
                   status, installed_by, installed_at, created_at, updated_at
            FROM plugins
            WHERE tenant_id = @tenant_id
            ORDER BY name ASC
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);

        var plugins = new List<Plugin>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            plugins.Add(MapPlugin(reader));
        }

        return plugins;
    }

    public async Task InstallAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO plugins (
                id, tenant_id, name, version, description, assembly_name, configuration_json,
                status, installed_by, installed_at, created_at, updated_at)
            VALUES (
                @id, @tenant_id, @name, @version, @description, @assembly_name, @configuration_json::jsonb,
                @status, @installed_by, @installed_at, @created_at, @updated_at)
            """,
            connection);

        AddParameters(command, plugin);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE plugins SET
                version = @version,
                description = @description,
                configuration_json = @configuration_json::jsonb,
                status = @status,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        AddParameters(command, plugin);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "DELETE FROM plugins WHERE tenant_id = @tenant_id AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, Plugin plugin)
    {
        command.Parameters.AddWithValue("id", plugin.Id);
        command.Parameters.AddWithValue("tenant_id", plugin.TenantId);
        command.Parameters.AddWithValue("name", plugin.Name);
        command.Parameters.AddWithValue("version", plugin.Version);
        command.Parameters.AddWithValue("description", plugin.Description);
        command.Parameters.AddWithValue("assembly_name", plugin.AssemblyName);
        command.Parameters.AddWithValue("configuration_json", plugin.ConfigurationJson);
        command.Parameters.AddWithValue("status", (int)plugin.Status);
        command.Parameters.AddWithValue("installed_by", (object?)plugin.InstalledBy ?? DBNull.Value);
        command.Parameters.AddWithValue("installed_at", plugin.InstalledAt);
        command.Parameters.AddWithValue("created_at", plugin.CreatedAt);
        command.Parameters.AddWithValue("updated_at", plugin.UpdatedAt);
    }

    private static Plugin MapPlugin(NpgsqlDataReader reader) =>
        Plugin.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetString(reader.GetOrdinal("version")),
            reader.GetString(reader.GetOrdinal("description")),
            reader.GetString(reader.GetOrdinal("assembly_name")),
            reader.GetString(reader.GetOrdinal("configuration_json")),
            (PluginStatus)reader.GetInt32(reader.GetOrdinal("status")),
            reader.IsDBNull(reader.GetOrdinal("installed_by"))
                ? null
                : reader.GetString(reader.GetOrdinal("installed_by")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("installed_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
}

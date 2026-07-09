namespace Sentinel.Api.Authorization;

using Sentinel.Api.Authentication;

public static class SentinelPermissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string UsersDelete = "users.delete";
    public const string TenantsRead = "tenants.read";
    public const string TenantsWrite = "tenants.write";
    public const string TenantsDelete = "tenants.delete";
    public const string LogsRead = "logs.read";
    public const string LogsWrite = "logs.write";
    public const string MetricsRead = "metrics.read";
    public const string MetricsWrite = "metrics.write";
    public const string TracesRead = "traces.read";
    public const string TracesWrite = "traces.write";
    public const string AlertsRead = "alerts.read";
    public const string AlertsWrite = "alerts.write";
    public const string AlertsDelete = "alerts.delete";
    public const string IncidentsRead = "incidents.read";
    public const string IncidentsWrite = "incidents.write";
    public const string IncidentsDelete = "incidents.delete";
    public const string DashboardsRead = "dashboards.read";
    public const string DashboardsWrite = "dashboards.write";
    public const string DashboardsDelete = "dashboards.delete";
    public const string PluginsRead = "plugins.read";
    public const string PluginsWrite = "plugins.write";
    public const string PluginsDelete = "plugins.delete";
    public const string SettingsRead = "settings.read";
    public const string SettingsWrite = "settings.write";
    public const string SettingsDelete = "settings.delete";
    public const string SearchRead = "search.read";
    public const string SearchWrite = "search.write";
    public const string IngestLogs = "logs.write";
    public const string IngestMetrics = "metrics.write";
    public const string IngestTraces = "traces.write";

    public static IReadOnlyList<string> All { get; } =
    [
        UsersRead, UsersWrite, UsersDelete,
        TenantsRead, TenantsWrite, TenantsDelete,
        LogsRead, LogsWrite,
        MetricsRead, MetricsWrite,
        TracesRead, TracesWrite,
        AlertsRead, AlertsWrite, AlertsDelete,
        IncidentsRead, IncidentsWrite, IncidentsDelete,
        DashboardsRead, DashboardsWrite, DashboardsDelete,
        PluginsRead, PluginsWrite, PluginsDelete,
        SettingsRead, SettingsWrite, SettingsDelete,
        SearchRead, SearchWrite,
    ];
}

public static class SentinelPolicies
{
    public const string UsersRead = SentinelPermissions.UsersRead;
    public const string UsersWrite = SentinelPermissions.UsersWrite;
    public const string UsersDelete = SentinelPermissions.UsersDelete;
    public const string TenantsRead = SentinelPermissions.TenantsRead;
    public const string TenantsWrite = SentinelPermissions.TenantsWrite;
    public const string TenantsDelete = SentinelPermissions.TenantsDelete;
    public const string LogsRead = SentinelPermissions.LogsRead;
    public const string LogsWrite = SentinelPermissions.LogsWrite;
    public const string MetricsRead = SentinelPermissions.MetricsRead;
    public const string MetricsWrite = SentinelPermissions.MetricsWrite;
    public const string TracesRead = SentinelPermissions.TracesRead;
    public const string TracesWrite = SentinelPermissions.TracesWrite;
    public const string AlertsRead = SentinelPermissions.AlertsRead;
    public const string AlertsWrite = SentinelPermissions.AlertsWrite;
    public const string AlertsDelete = SentinelPermissions.AlertsDelete;
    public const string IncidentsRead = SentinelPermissions.IncidentsRead;
    public const string IncidentsWrite = SentinelPermissions.IncidentsWrite;
    public const string IncidentsDelete = SentinelPermissions.IncidentsDelete;
    public const string DashboardsRead = SentinelPermissions.DashboardsRead;
    public const string DashboardsWrite = SentinelPermissions.DashboardsWrite;
    public const string DashboardsDelete = SentinelPermissions.DashboardsDelete;
    public const string PluginsRead = SentinelPermissions.PluginsRead;
    public const string PluginsWrite = SentinelPermissions.PluginsWrite;
    public const string PluginsDelete = SentinelPermissions.PluginsDelete;
    public const string SettingsRead = SentinelPermissions.SettingsRead;
    public const string SettingsWrite = SentinelPermissions.SettingsWrite;
    public const string SettingsDelete = SentinelPermissions.SettingsDelete;
    public const string SearchRead = SentinelPermissions.SearchRead;
    public const string SearchWrite = SentinelPermissions.SearchWrite;
    public const string Ingestion = "ingestion";
}

public static class AuthorizationExtensions
{
    public static IServiceCollection AddSentinelAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in SentinelPermissions.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim("permission", permission));
            }

            options.AddPolicy(SentinelPolicies.Ingestion, policy =>
            {
                policy.AddAuthenticationSchemes(
                    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }
}

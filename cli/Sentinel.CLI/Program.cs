using System.CommandLine;
using Sentinel.CLI;

var root = new RootCommand("Sentinel CLI — manage logs, alerts, incidents, and tenants.")
{
    GlobalOptions.BaseUrlOption,
    GlobalOptions.OutputOption,
    AuthCommands.Create(),
    LogsCommands.Create(),
    AlertsCommands.Create(),
    IncidentsCommands.Create(),
    TenantsCommands.Create()
};

return await root.InvokeAsync(args);

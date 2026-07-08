using System.CommandLine;
using System.CommandLine.Invocation;
using Sentinel.Sdk;
using Sentinel.Sdk.Models;

namespace Sentinel.CLI;

internal static class ConfigStore
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".sentinel");

    private static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");

    public static SentinelClientOptions LoadOptions(string? baseUrlOverride)
    {
        var options = new SentinelClientOptions();
        if (File.Exists(ConfigFile))
        {
            var json = File.ReadAllText(ConfigFile);
            var stored = System.Text.Json.JsonSerializer.Deserialize<StoredConfig>(json);
            if (stored is not null)
            {
                options.BaseUrl = stored.BaseUrl ?? options.BaseUrl;
                options.AccessToken = stored.AccessToken;
                options.RefreshToken = stored.RefreshToken;
                options.TenantId = stored.TenantId;
            }
        }

        if (!string.IsNullOrWhiteSpace(baseUrlOverride))
        {
            options.BaseUrl = baseUrlOverride;
        }

        return options;
    }

    public static void Save(AuthResponse auth, string baseUrl)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var stored = new StoredConfig
        {
            BaseUrl = baseUrl,
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken,
            TenantId = auth.User.TenantId,
            Email = auth.User.Email
        };

        var json = System.Text.Json.JsonSerializer.Serialize(stored, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigFile, json);
    }

    private sealed class StoredConfig
    {
        public string? BaseUrl { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public Guid? TenantId { get; set; }
        public string? Email { get; set; }
    }
}

internal static class CommandContext
{
    public static SentinelClient CreateClient(InvocationContext context)
    {
        var baseUrl = context.ParseResult.GetValueForOption(GlobalOptions.BaseUrlOption);
        return new SentinelClient(ConfigStore.LoadOptions(baseUrl));
    }

    public static OutputFormat GetOutputFormat(InvocationContext context)
    {
        var value = context.ParseResult.GetValueForOption(GlobalOptions.OutputOption);
        return value?.Equals("yaml", StringComparison.OrdinalIgnoreCase) == true
            ? OutputFormat.Yaml
            : OutputFormat.Json;
    }
}

internal static class GlobalOptions
{
    public static readonly Option<string> BaseUrlOption = new("--base-url", () => SentinelClientOptions.DefaultBaseUrl, "Sentinel API base URL.");
    public static readonly Option<string> OutputOption = new("--output", () => "json", "Output format: json or yaml.");
}

internal static class AuthCommands
{
    public static Command Create()
    {
        var auth = new Command("auth", "Authentication commands.");
        var login = new Command("login", "Authenticate with Sentinel and store credentials locally.");
        var email = new Option<string>("--email", "Account email.") { IsRequired = true };
        var password = new Option<string>("--password", "Account password.") { IsRequired = true };
        var tenant = new Option<Guid?>("--tenant-id", "Optional tenant ID.");

        login.AddOption(email);
        login.AddOption(password);
        login.AddOption(tenant);
        login.SetHandler(async (context) =>
        {
            var format = CommandContext.GetOutputFormat(context);
            try
            {
                var client = CommandContext.CreateClient(context);
                var authResponse = await client.LoginAsync(new LoginRequest(
                    context.ParseResult.GetValueForOption(email)!,
                    context.ParseResult.GetValueForOption(password)!,
                    context.ParseResult.GetValueForOption(tenant)));

                var baseUrl = context.ParseResult.GetValueForOption(GlobalOptions.BaseUrlOption)!;
                ConfigStore.Save(authResponse, baseUrl);
                OutputFormatter.Write(new
                {
                    status = "authenticated",
                    user = authResponse.User,
                    expiresAt = authResponse.AccessTokenExpiresAt
                }, format);
            }
            catch (SentinelApiException ex)
            {
                OutputFormatter.WriteError(ex.Message, format);
            }
        });

        auth.AddCommand(login);
        return auth;
    }
}

internal static class LogsCommands
{
    public static Command Create()
    {
        var logs = new Command("logs", "Log operations.");
        var search = new Command("search", "Search ingested logs.");
        var query = new Option<string?>("--query", "Full-text search query.");
        var level = new Option<string?>("--level", "Log level filter.");
        var service = new Option<string?>("--service", "Service name filter.");
        var environment = new Option<string?>("--environment", "Environment filter.");
        var from = new Option<DateTimeOffset?>("--from", "Start timestamp (ISO 8601).");
        var to = new Option<DateTimeOffset?>("--to", "End timestamp (ISO 8601).");
        var limit = new Option<int>("--limit", () => 100, "Maximum results.");
        var offset = new Option<int>("--offset", () => 0, "Result offset.");

        search.AddOption(query);
        search.AddOption(level);
        search.AddOption(service);
        search.AddOption(environment);
        search.AddOption(from);
        search.AddOption(to);
        search.AddOption(limit);
        search.AddOption(offset);
        search.SetHandler(async (context) =>
        {
            var format = CommandContext.GetOutputFormat(context);
            try
            {
                var client = CommandContext.CreateClient(context);
                var result = await client.SearchLogsAsync(new LogSearchQuery(
                    context.ParseResult.GetValueForOption(from),
                    context.ParseResult.GetValueForOption(to),
                    context.ParseResult.GetValueForOption(level),
                    context.ParseResult.GetValueForOption(service),
                    context.ParseResult.GetValueForOption(environment),
                    context.ParseResult.GetValueForOption(query),
                    null,
                    context.ParseResult.GetValueForOption(limit),
                    context.ParseResult.GetValueForOption(offset)));

                OutputFormatter.Write(result, format);
            }
            catch (SentinelApiException ex)
            {
                OutputFormatter.WriteError(ex.Message, format);
            }
        });

        logs.AddCommand(search);
        return logs;
    }
}

internal static class AlertsCommands
{
    public static Command Create()
    {
        var alerts = new Command("alerts", "Alert rule operations.");
        var list = new Command("list", "List alert rules.");

        list.SetHandler(async (context) =>
        {
            var format = CommandContext.GetOutputFormat(context);
            try
            {
                var client = CommandContext.CreateClient(context);
                var rules = await client.ListAlertsAsync();
                OutputFormatter.Write(rules, format);
            }
            catch (SentinelApiException ex)
            {
                OutputFormatter.WriteError(ex.Message, format);
            }
        });

        alerts.AddCommand(list);
        return alerts;
    }
}

internal static class IncidentsCommands
{
    public static Command Create()
    {
        var incidents = new Command("incidents", "Incident operations.");
        var show = new Command("show", "Show incident details.");
        var id = new Argument<Guid>("id", "Incident ID.");

        show.AddArgument(id);
        show.SetHandler(async (context) =>
        {
            var format = CommandContext.GetOutputFormat(context);
            try
            {
                var client = CommandContext.CreateClient(context);
                var incident = await client.GetIncidentAsync(context.ParseResult.GetValueForArgument(id));
                OutputFormatter.Write(incident, format);
            }
            catch (SentinelApiException ex)
            {
                OutputFormatter.WriteError(ex.Message, format);
            }
        });

        incidents.AddCommand(show);
        return incidents;
    }
}

internal static class TenantsCommands
{
    public static Command Create()
    {
        var tenants = new Command("tenants", "Tenant operations.");
        var list = new Command("list", "List accessible tenants.");

        list.SetHandler(async (context) =>
        {
            var format = CommandContext.GetOutputFormat(context);
            try
            {
                var client = CommandContext.CreateClient(context);
                var result = await client.ListTenantsAsync();
                OutputFormatter.Write(result, format);
            }
            catch (SentinelApiException ex)
            {
                OutputFormatter.WriteError(ex.Message, format);
            }
        });

        tenants.AddCommand(list);
        return tenants;
    }
}

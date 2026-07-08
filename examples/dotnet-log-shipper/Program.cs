using Sentinel.Sdk;
using Sentinel.Sdk.Models;

var baseUrl = Environment.GetEnvironmentVariable("SENTINEL_BASE_URL") ?? SentinelClientOptions.DefaultBaseUrl;
var tenantIdValue = Environment.GetEnvironmentVariable("SENTINEL_TENANT_ID");
var accessToken = Environment.GetEnvironmentVariable("SENTINEL_ACCESS_TOKEN");

var options = new SentinelClientOptions
{
    BaseUrl = baseUrl,
    AccessToken = accessToken,
    TenantId = Guid.TryParse(tenantIdValue, out var tenantId) ? tenantId : null
};

if (string.IsNullOrWhiteSpace(options.AccessToken))
{
    var email = Environment.GetEnvironmentVariable("SENTINEL_EMAIL")
        ?? throw new InvalidOperationException("Set SENTINEL_ACCESS_TOKEN or SENTINEL_EMAIL and SENTINEL_PASSWORD.");
    var password = Environment.GetEnvironmentVariable("SENTINEL_PASSWORD")
        ?? throw new InvalidOperationException("SENTINEL_PASSWORD is required when SENTINEL_ACCESS_TOKEN is not set.");

    using var authClient = new SentinelClient(options);
    var auth = await authClient.LoginAsync(new LoginRequest(email, password, options.TenantId));
    options.AccessToken = auth.AccessToken;
    options.TenantId = auth.User.TenantId;
}

using var client = new SentinelClient(options);

var service = Environment.GetEnvironmentVariable("SENTINEL_SERVICE") ?? "dotnet-log-shipper";
var environment = Environment.GetEnvironmentVariable("SENTINEL_ENVIRONMENT") ?? "development";

var response = await client.IngestLogsAsync(new IngestLogsRequest(
[
    new LogEntryInput(
        service,
        environment,
        "info",
        "dotnet-log-shipper started",
        new Dictionary<string, string> { ["host"] = Environment.MachineName }),
    new LogEntryInput(
        service,
        environment,
        "warning",
        "sample warning event shipped to Sentinel",
        new Dictionary<string, string> { ["sample"] = "true" })
]));

Console.WriteLine($"Shipped {response.AcceptedCount} log entries ({response.Status}).");

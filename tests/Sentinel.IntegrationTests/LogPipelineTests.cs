using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public class LogPipelineTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public LogPipelineTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
    }

    [Fact]
    public async Task IngestAndSearch_EndToEnd_ReturnsIngestedLogs()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = TestData.AdminPassword,
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = login.GetProperty("accessToken").GetString()!;
        var tenantId = Guid.Parse(login.GetProperty("user").GetProperty("tenantId").GetString()!);

        var authedClient = _factory.CreateAuthenticatedClient(accessToken, tenantId);

        var ingestResponse = await authedClient.PostAsJsonAsync("/api/v1/logs", new
        {
            logs = new[]
            {
                new
                {
                    service = "checkout",
                    environment = "production",
                    level = "error",
                    message = "Payment gateway timeout during checkout",
                    timestamp = DateTimeOffset.UtcNow,
                },
            },
        });

        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);

        var searchResponse = await authedClient.GetAsync(
            "/api/v1/search/logs?query=checkout&level=error&limit=10");

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var search = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(search.GetProperty("totalCount").GetInt64() >= 1);
        var first = search.GetProperty("items")[0];
        Assert.Contains("checkout", first.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ingest_WithoutTenant_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/logs", new
        {
            logs = new[]
            {
                new
                {
                    service = "api",
                    environment = "dev",
                    level = "info",
                    message = "test",
                },
            },
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

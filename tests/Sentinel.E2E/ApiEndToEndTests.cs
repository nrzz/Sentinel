using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.E2E;

public class ApiEndToEndTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public ApiEndToEndTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
    }

    [Fact]
    public async Task FullObservabilityWorkflow_LoginIngestSearchCreateAlert_Succeeds()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = TestData.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
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
                    service = "payments",
                    environment = "production",
                    level = "error",
                    message = "Card declined for order 12345",
                },
                new
                {
                    service = "payments",
                    environment = "production",
                    level = "info",
                    message = "Retry succeeded for order 12345",
                },
            },
        });
        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);

        var searchResponse = await authedClient.GetAsync("/api/v1/search/logs?query=order&limit=20");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var search = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, search.GetProperty("totalCount").GetInt64());

        var alertResponse = await authedClient.PostAsJsonAsync("/api/v1/alerts", new
        {
            name = "Payment failures",
            description = "Alert on payment errors",
            query = "level:error service:payments",
            condition = "count > 10",
            severity = 2,
            evaluationIntervalSeconds = 300,
            notificationChannels = new[] { "email" },
        });
        Assert.Equal(HttpStatusCode.Created, alertResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = login.GetProperty("refreshToken").GetString(),
        });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
    }
}

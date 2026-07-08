using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public class AlertFlowTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public AlertFlowTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
    }

    [Fact]
    public async Task CreateAndListAlert_EndToEnd_Succeeds()
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

        var createResponse = await authedClient.PostAsJsonAsync("/api/v1/alerts", new
        {
            name = "High error rate",
            description = "Triggers when error logs exceed threshold",
            query = "level:error",
            condition = "error_rate > 5",
            severity = 2,
            evaluationIntervalSeconds = 60,
            notificationChannels = new[] { "slack" },
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await authedClient.GetAsync("/api/v1/alerts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var alerts = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(alerts.GetArrayLength() >= 1);
    }
}

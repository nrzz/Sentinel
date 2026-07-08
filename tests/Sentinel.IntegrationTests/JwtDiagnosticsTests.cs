using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Infrastructure.Identity;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public class JwtDiagnosticsTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public JwtDiagnosticsTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
    }

    [Fact]
    public async Task JwtToken_ValidatesWithConfiguredService()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = TestData.AdminPassword,
        });

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = login.GetProperty("accessToken").GetString()!;

        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var principal = jwtService.ValidateAccessToken(accessToken);

        Assert.NotNull(principal);
        Assert.True(principal!.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithBearerToken_ReturnsOk()
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
        var response = await authedClient.GetAsync("/api/v1/search/logs?limit=10");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK but got {response.StatusCode}: {body}");
    }
}

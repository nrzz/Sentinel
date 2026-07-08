using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public class AuthFlowTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public AuthFlowTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = TestData.AdminPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("accessToken", out var token));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
        Assert.True(json.TryGetProperty("expiresIn", out var expiresIn));
        Assert.True(expiresIn.GetInt32() > 0);
        Assert.Equal(TestData.AdminEmail, json.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = "wrong-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessToken()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestData.AdminEmail,
            password = TestData.AdminPassword,
        });

        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = login.GetProperty("refreshToken").GetString();

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken,
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(refreshed.GetProperty("accessToken").GetString()));
    }
}

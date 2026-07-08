using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sentinel.IntegrationTests.Infrastructure;

namespace Sentinel.IntegrationTests;

public class HealthEndpointTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(SentinelWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOkWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task Live_ReturnsOk()
    {
        var response = await _client.GetAsync("/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

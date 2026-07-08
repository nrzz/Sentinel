namespace Sentinel.E2E;

public sealed class BrowserE2EFixture : IAsyncLifetime
{
    public static string BaseUrl { get; private set; } = "http://localhost:5173";
    public static bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("SENTINEL_E2E_URL");
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            IsAvailable = false;
            return;
        }

        BaseUrl = configuredUrl.TrimEnd('/');
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            var response = await client.GetAsync($"{BaseUrl}/login");
            IsAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

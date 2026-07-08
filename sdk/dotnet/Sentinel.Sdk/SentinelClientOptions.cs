namespace Sentinel.Sdk;

public sealed class SentinelClientOptions
{
    public const string DefaultBaseUrl = "http://localhost:5000";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public Guid? TenantId { get; set; }
    public string TenantHeaderName { get; set; } = "X-Tenant-ID";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryAttempts { get; set; } = 3;
}

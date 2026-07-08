using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Sentinel.Sdk.Models;

namespace Sentinel.Sdk;

public sealed class SentinelClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly SentinelClientOptions _options;

    public SentinelClient(SentinelClientOptions options)
        : this(options, CreateDefaultHttpClient(options))
    {
        _ownsHttpClient = true;
    }

    public SentinelClient(SentinelClientOptions options, HttpClient httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.BaseAddress ??= new Uri(options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = options.Timeout;
        ApplyAuthHeaders();
    }

    public SentinelClientOptions Options => _options;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostAsync<AuthResponse>("/api/v1/auth/login", request, authenticated: false, cancellationToken);
        SetTokens(response.AccessToken, response.RefreshToken, response.User.TenantId);
        return response;
    }

    public async Task<AuthResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.RefreshToken))
        {
            throw new InvalidOperationException("Refresh token is not configured.");
        }

        var response = await PostAsync<AuthResponse>(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(_options.RefreshToken),
            authenticated: false,
            cancellationToken);

        SetTokens(response.AccessToken, response.RefreshToken, response.User.TenantId);
        return response;
    }

    public void SetAccessToken(string accessToken, Guid? tenantId = null)
    {
        _options.AccessToken = accessToken;
        if (tenantId.HasValue)
        {
            _options.TenantId = tenantId;
        }

        ApplyAuthHeaders();
    }

    public async Task<IngestLogsResponse> IngestLogsAsync(
        IngestLogsRequest request,
        CancellationToken cancellationToken = default) =>
        await PostAsync<IngestLogsResponse>("/api/v1/logs", request, authenticated: true, cancellationToken);

    public async Task<SearchLogsResponse> SearchLogsAsync(
        LogSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(new Dictionary<string, string?>
        {
            ["from"] = query.From?.ToString("O"),
            ["to"] = query.To?.ToString("O"),
            ["level"] = query.Level,
            ["service"] = query.Service,
            ["environment"] = query.Environment,
            ["query"] = query.Query,
            ["traceId"] = query.TraceId,
            ["limit"] = query.Limit.ToString(),
            ["offset"] = query.Offset.ToString()
        });

        return await GetAsync<SearchLogsResponse>($"/api/v1/search/logs{queryString}", cancellationToken);
    }

    public async Task<IngestMetricsResponse> IngestMetricsAsync(
        IngestMetricsRequest request,
        CancellationToken cancellationToken = default) =>
        await PostAsync<IngestMetricsResponse>("/api/v1/metrics", request, authenticated: true, cancellationToken);

    public async Task<QueryMetricsResponse> QueryMetricsAsync(
        MetricQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(new Dictionary<string, string?>
        {
            ["from"] = query.From?.ToString("O"),
            ["to"] = query.To?.ToString("O"),
            ["name"] = query.Name,
            ["service"] = query.Service,
            ["environment"] = query.Environment,
            ["limit"] = query.Limit.ToString()
        });

        return await GetAsync<QueryMetricsResponse>($"/api/v1/metrics{queryString}", cancellationToken);
    }

    public async Task<IReadOnlyList<AlertRule>> ListAlertsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<IReadOnlyList<AlertRule>>("/api/v1/alerts", cancellationToken);

    public async Task<AlertRule> GetAlertAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetAsync<AlertRule>($"/api/v1/alerts/{id}", cancellationToken);

    public async Task<IReadOnlyList<AlertExecution>> ListAlertExecutionsAsync(
        Guid alertRuleId,
        int limit = 100,
        CancellationToken cancellationToken = default) =>
        await GetAsync<IReadOnlyList<AlertExecution>>($"/api/v1/alerts/{alertRuleId}/executions?limit={limit}", cancellationToken);

    public async Task<IReadOnlyList<Incident>> ListIncidentsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<IReadOnlyList<Incident>>("/api/v1/incidents", cancellationToken);

    public async Task<Incident> GetIncidentAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetAsync<Incident>($"/api/v1/incidents/{id}", cancellationToken);

    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<IReadOnlyList<Tenant>>("/api/v1/tenants", cancellationToken);

    public async Task<Tenant> GetTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetAsync<Tenant>($"/api/v1/tenants/{id}", cancellationToken);

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private void SetTokens(string accessToken, string refreshToken, Guid tenantId)
    {
        _options.AccessToken = accessToken;
        _options.RefreshToken = refreshToken;
        _options.TenantId = tenantId;
        ApplyAuthHeaders();
    }

    private void ApplyAuthHeaders()
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(_options.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        _httpClient.DefaultRequestHeaders.Remove(_options.TenantHeaderName);
        if (_options.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(_options.TenantHeaderName, _options.TenantId.Value.ToString());
        }
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string path, object body, bool authenticated, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, mediaType: null, JsonOptions)
        };

        if (!authenticated)
        {
            request.Headers.Authorization = null;
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions)
                ?? throw new InvalidOperationException("Sentinel API returned an empty response body.");
        }

        var message = TryReadErrorMessage(body) ?? response.ReasonPhrase ?? "Request failed.";
        throw new SentinelApiException((int)response.StatusCode, message, body);
    }

    private static string? TryReadErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?> parameters)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append(first ? '?' : '&');
            first = false;
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
        }

        return builder.ToString();
    }

    private static HttpClient CreateDefaultHttpClient(SentinelClientOptions options)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

        var handler = new PolicyHttpMessageHandler(retryPolicy)
        {
            InnerHandler = new HttpClientHandler()
        };

        return new HttpClient(handler);
    }
}

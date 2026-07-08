using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.AI;

public sealed class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiProvider(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAi;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public string ProviderName => "openai";
    public string ModelName => _options.Model;

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var payload = new
        {
            model = _options.Model,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI returned an empty response.");

        stopwatch.Stop();
        var content = body.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        return new AiCompletionResponse(content, ProviderName, ModelName, stopwatch.ElapsedMilliseconds);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var payload = new { model = _options.EmbeddingModel, input = text };
        using var response = await _httpClient.PostAsJsonAsync("embeddings", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("OpenAI returned an empty embedding response.");

        return body.Data?.FirstOrDefault()?.Embedding ?? [];
    }

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; init; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; init; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private sealed class OpenAiEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiEmbeddingData>? Data { get; init; }
    }

    private sealed class OpenAiEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; init; }
    }
}

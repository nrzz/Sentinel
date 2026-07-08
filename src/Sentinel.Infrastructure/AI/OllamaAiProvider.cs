using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.AI;

public sealed class OllamaAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaAiProvider(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Ollama;
    }

    public string ProviderName => "ollama";
    public string ModelName => _options.Model;

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var payload = new
        {
            model = _options.Model,
            stream = false,
            options = new { temperature = request.Temperature, num_predict = request.MaxTokens },
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/chat", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        stopwatch.Stop();
        return new AiCompletionResponse(
            body.Message?.Content ?? string.Empty,
            ProviderName,
            ModelName,
            stopwatch.ElapsedMilliseconds);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var payload = new { model = _options.EmbeddingModel, input = text };
        using var response = await _httpClient.PostAsJsonAsync("/api/embeddings", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");

        return body.Embedding ?? [];
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; init; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; init; }
    }
}

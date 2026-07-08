using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Sentinel.PluginSdk;

namespace Sentinel.Plugins.OllamaAi;

public sealed class OllamaAiPlugin : PluginBase, IAIProviderPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public override string Id => "sentinel.plugins.ollama-ai";
    public override string Name => "Ollama AI Provider";
    public override string Version => "1.0.0";
    public override string Description => "Provides text completion and embeddings through a local or remote Ollama instance.";
    public override string Author => "Sentinel";
    public override IReadOnlyList<string> Tags => ["ai", "ollama", "llm", "embeddings"];

    public override IReadOnlyDictionary<string, ConfigSchemaProperty> GetConfigSchema() =>
        new Dictionary<string, ConfigSchemaProperty>
        {
            ["baseUrl"] = new("baseUrl", "string", "Ollama API base URL.", false, "http://localhost:11434"),
            ["model"] = new("model", "string", "Default completion model.", false, "llama3.2"),
            ["embeddingModel"] = new("embeddingModel", "string", "Default embedding model.", false, "nomic-embed-text")
        };

    public override PluginHealthResult CheckHealth()
    {
        var baseUrl = GetSetting("baseUrl", "http://localhost:11434");
        try
        {
            using var response = HttpClient.GetAsync($"{baseUrl.TrimEnd('/')}/api/tags").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode
                ? new PluginHealthResult(PluginHealthStatus.Healthy, "Ollama API is reachable.")
                : new PluginHealthResult(PluginHealthStatus.Degraded, $"Ollama API returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new PluginHealthResult(PluginHealthStatus.Unhealthy, $"Cannot reach Ollama API: {ex.Message}");
        }
    }

    public async Task<AiCompletionResponse> CompleteAsync(
        AiCompletionRequest request,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);

        var baseUrl = settings.GetValueOrDefault("baseUrl", GetSetting("baseUrl", "http://localhost:11434"));
        var model = settings.GetValueOrDefault("model", GetSetting("model", "llama3.2"));
        var stopwatch = Stopwatch.StartNew();

        var payload = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = request.Prompt,
            System = request.SystemPrompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

        using var response = await HttpClient.PostAsJsonAsync(
            $"{baseUrl.TrimEnd('/')}/api/generate",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty completion response.");

        stopwatch.Stop();
        return new AiCompletionResponse(
            result.Response,
            model,
            result.PromptEvalCount ?? 0,
            result.EvalCount ?? 0,
            stopwatch.Elapsed);
    }

    public async Task<AiEmbeddingResponse> EmbedAsync(
        AiEmbeddingRequest request,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);

        var baseUrl = settings.GetValueOrDefault("baseUrl", GetSetting("baseUrl", "http://localhost:11434"));
        var model = request.Model
            ?? settings.GetValueOrDefault("embeddingModel", GetSetting("embeddingModel", "nomic-embed-text"));

        var embeddings = new List<IReadOnlyList<float>>();
        foreach (var input in request.Inputs)
        {
            var payload = new OllamaEmbedRequest { Model = model, Input = input };
            using var response = await HttpClient.PostAsJsonAsync(
                $"{baseUrl.TrimEnd('/')}/api/embeddings",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");

            embeddings.Add(result.Embedding);
        }

        var dimensions = embeddings.FirstOrDefault()?.Count ?? 0;
        return new AiEmbeddingResponse(embeddings, model, dimensions);
    }

    private sealed class OllamaGenerateRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string? System { get; set; }
        public bool Stream { get; set; }
        public OllamaOptions? Options { get; set; }
    }

    private sealed class OllamaOptions
    {
        public double Temperature { get; set; }
        public int NumPredict { get; set; }
    }

    private sealed class OllamaGenerateResponse
    {
        public string Response { get; set; } = string.Empty;
        public int? PromptEvalCount { get; set; }
        public int? EvalCount { get; set; }
    }

    private sealed class OllamaEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("embedding")]
        public List<float> Embedding { get; set; } = [];
    }
}

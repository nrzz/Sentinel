namespace Sentinel.PluginSdk;

public sealed record AiCompletionRequest(
    string Prompt,
    string? SystemPrompt,
    double Temperature,
    int MaxTokens,
    IReadOnlyDictionary<string, string>? Context);

public sealed record AiCompletionResponse(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration);

public sealed record AiEmbeddingRequest(
    IReadOnlyList<string> Inputs,
    string? Model);

public sealed record AiEmbeddingResponse(
    IReadOnlyList<IReadOnlyList<float>> Embeddings,
    string Model,
    int Dimensions);

/// <summary>Provides AI inference capabilities to Sentinel features.</summary>
public interface IAIProviderPlugin : IPluginMetadata
{
    Task<AiCompletionResponse> CompleteAsync(
        AiCompletionRequest request,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);

    Task<AiEmbeddingResponse> EmbedAsync(
        AiEmbeddingRequest request,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}

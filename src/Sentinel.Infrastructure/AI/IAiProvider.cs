namespace Sentinel.Infrastructure.AI;

public sealed record AiCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    double Temperature = 0.3,
    int MaxTokens = 4096);

public sealed record AiCompletionResponse(
    string Content,
    string Provider,
    string Model,
    long LatencyMs);

public interface IAiProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default);
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

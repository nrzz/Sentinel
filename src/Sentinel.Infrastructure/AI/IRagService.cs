using Sentinel.Domain.AI;

namespace Sentinel.Infrastructure.AI;

public sealed record RagDocument(
    string Id,
    string Title,
    string Content,
    string SourceType,
    float[]? Embedding = null);

public sealed record RagSearchResult(
    string Answer,
    IReadOnlyList<CorrelationSource> Sources,
    string PromptTemplateId,
    string PromptTemplateVersion);

public interface IRagService
{
    Task IndexDocumentsAsync(IReadOnlyList<RagDocument> documents, CancellationToken cancellationToken = default);
    Task<RagSearchResult> SearchAsync(
        Guid tenantId,
        string question,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default);
}

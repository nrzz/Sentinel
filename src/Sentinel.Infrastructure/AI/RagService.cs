using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sentinel.Domain.AI;
using Sentinel.Domain.Configuration;

namespace Sentinel.Infrastructure.AI;

public sealed class RagService : IRagService
{
    private readonly IAiProvider _provider;
    private readonly IEmbeddingService _embeddingService;
    private readonly IPromptCatalog _promptCatalog;
    private readonly IAiAuditService _auditService;
    private readonly AiOptions _options;
    private readonly List<RagDocument> _documents = [];

    public RagService(
        IAiProvider provider,
        IEmbeddingService embeddingService,
        IPromptCatalog promptCatalog,
        IAiAuditService auditService,
        IOptions<AiOptions> options)
    {
        _provider = provider;
        _embeddingService = embeddingService;
        _promptCatalog = promptCatalog;
        _auditService = auditService;
        _options = options.Value;
    }

    public async Task IndexDocumentsAsync(IReadOnlyList<RagDocument> documents, CancellationToken cancellationToken = default)
    {
        foreach (var document in documents)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(document.Content, cancellationToken);
            _documents.Add(document with { Embedding = embedding });
        }
    }

    public async Task<RagSearchResult> SearchAsync(
        Guid tenantId,
        string question,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var template = _promptCatalog.GetTemplate("rag-search")
            ?? throw new InvalidOperationException("RAG search prompt template is not configured.");

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);
        var ranked = _documents
            .Where(d => d.Embedding is not null)
            .Select(d => new
            {
                Document = d,
                Score = _embeddingService.CosineSimilarity(queryEmbedding, d.Embedding!)
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        var sources = ranked.Select((x, index) => new CorrelationSource
        {
            SourceType = x.Document.SourceType,
            SourceId = x.Document.Id,
            Title = x.Document.Title,
            Excerpt = x.Document.Content.Length > 240
                ? x.Document.Content[..240] + "..."
                : x.Document.Content,
            Citation = $"[source:{index + 1}] {x.Document.Title} ({x.Document.SourceType}:{x.Document.Id})"
        }).ToList();

        var contextBuilder = new StringBuilder();
        for (var i = 0; i < sources.Count; i++)
        {
            contextBuilder.AppendLine($"[source:{i + 1}] {ranked[i].Document.Title}");
            contextBuilder.AppendLine(ranked[i].Document.Content);
            contextBuilder.AppendLine();
        }

        var userPrompt = template.Render(new Dictionary<string, string>
        {
            ["question"] = question,
            ["context"] = contextBuilder.ToString()
        });

        var request = new AiCompletionRequest(
            template.SystemPrompt,
            userPrompt,
            _options.Temperature,
            _options.MaxTokens);

        try
        {
            var response = await _provider.CompleteAsync(request, cancellationToken);
            var answer = AppendSourceCitations(response.Content, sources);

            await _auditService.RecordAsync(
                tenantId,
                AiInteractionType.Search,
                template.Id,
                template.Version,
                JsonSerializer.Serialize(new { question, context = contextBuilder.ToString() }),
                response with { Content = answer },
                sources,
                correlationId,
                userId,
                cancellationToken);

            return new RagSearchResult(answer, sources, template.Id, template.Version);
        }
        catch (Exception ex)
        {
            await _auditService.RecordFailureAsync(
                tenantId,
                AiInteractionType.Search,
                _provider.ProviderName,
                _provider.ModelName,
                JsonSerializer.Serialize(new { question }),
                ex.Message,
                correlationId,
                userId,
                cancellationToken);
            throw;
        }
    }

    private static string AppendSourceCitations(string answer, IReadOnlyList<CorrelationSource> sources)
    {
        if (sources.Count == 0)
        {
            return answer;
        }

        var builder = new StringBuilder(answer.Trim());
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("Sources:");
        foreach (var source in sources)
        {
            builder.AppendLine($"- {source.Citation}");
        }

        return builder.ToString();
    }
}

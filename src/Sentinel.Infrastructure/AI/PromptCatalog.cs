using Sentinel.Domain.AI;

namespace Sentinel.Infrastructure.AI;

public sealed class PromptCatalog : IPromptCatalog
{
    private static readonly IReadOnlyList<PromptTemplate> Templates =
    [
        new()
        {
            Id = "rag-search",
            Version = "1.0.0",
            Name = "RAG Search",
            Description = "Answer observability questions using retrieved context with source citations.",
            SystemPrompt = """
                You are Sentinel, an observability assistant. Answer using only the provided context.
                Always cite sources using [source:N] notation where N matches the source index.
                If context is insufficient, say so explicitly.
                """,
            UserPromptTemplate = """
                Question: {{question}}

                Context:
                {{context}}

                Provide a concise answer with citations.
                """,
            RequiredVariables = ["question", "context"],
            RequiresSourceCitation = true
        },
        new()
        {
            Id = "rag-search",
            Version = "1.1.0",
            Name = "RAG Search",
            Description = "Enhanced RAG search with structured citations.",
            SystemPrompt = """
                You are Sentinel, an observability assistant.
                Use only the provided context. Cite every factual claim as [source:N].
                End with a 'Sources' section listing cited source titles.
                """,
            UserPromptTemplate = """
                Question: {{question}}

                Context:
                {{context}}

                Answer with inline citations and a Sources section.
                """,
            RequiredVariables = ["question", "context"],
            RequiresSourceCitation = true
        },
        new()
        {
            Id = "summarize",
            Version = "1.0.0",
            Name = "Summarize",
            Description = "Summarize logs, incidents, or alert activity.",
            SystemPrompt = """
                You are Sentinel. Produce concise operational summaries for on-call engineers.
                Highlight severity, impact, and recommended next steps.
                """,
            UserPromptTemplate = """
                Summarize the following {{content_type}}:

                {{content}}
                """,
            RequiredVariables = ["content_type", "content"],
            RequiresSourceCitation = false
        },
        new()
        {
            Id = "correlate",
            Version = "1.0.0",
            Name = "Correlate",
            Description = "Correlate incidents, alerts, and telemetry signals.",
            SystemPrompt = """
                You are Sentinel. Identify correlations between observability signals.
                Return a structured narrative with confidence indicators and cited evidence.
                """,
            UserPromptTemplate = """
                Correlate the following signals:

                {{signals}}
                """,
            RequiredVariables = ["signals"],
            RequiresSourceCitation = true
        }
    ];

    public PromptTemplate? GetTemplate(string id, string? version = null)
    {
        var matches = Templates.Where(t => t.Id == id).ToList();
        if (matches.Count == 0)
        {
            return null;
        }

        if (version is not null)
        {
            return matches.FirstOrDefault(t => t.Version == version);
        }

        return matches
            .OrderByDescending(t => Version.Parse(t.Version.Split('-')[0]))
            .First();
    }

    public IReadOnlyList<PromptTemplate> ListTemplates() => Templates;

    public IReadOnlyList<string> GetVersions(string id) =>
        Templates.Where(t => t.Id == id).Select(t => t.Version).OrderDescending().ToList();
}

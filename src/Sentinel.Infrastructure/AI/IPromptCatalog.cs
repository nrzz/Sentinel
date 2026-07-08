using Sentinel.Domain.AI;

namespace Sentinel.Infrastructure.AI;

public interface IPromptCatalog
{
    PromptTemplate? GetTemplate(string id, string? version = null);
    IReadOnlyList<PromptTemplate> ListTemplates();
    IReadOnlyList<string> GetVersions(string id);
}

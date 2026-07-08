namespace Sentinel.Domain.AI;

public sealed class PromptTemplate
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPromptTemplate { get; init; } = string.Empty;
    public IReadOnlyList<string> RequiredVariables { get; init; } = [];
    public bool RequiresSourceCitation { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string Render(IDictionary<string, string> variables)
    {
        var missing = RequiredVariables.Where(v => !variables.ContainsKey(v)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required template variables: {string.Join(", ", missing)}");
        }

        var rendered = UserPromptTemplate;
        foreach (var (key, value) in variables)
        {
            rendered = rendered.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
        }

        return rendered;
    }
}

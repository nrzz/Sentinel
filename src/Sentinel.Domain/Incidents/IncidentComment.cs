using Sentinel.Domain.Common;

namespace Sentinel.Domain.Incidents;

public sealed class IncidentComment : Entity
{
    public Guid TenantId { get; private set; }
    public Guid IncidentId { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }

    private IncidentComment() { }

    public static IncidentComment Create(
        Guid tenantId,
        Guid incidentId,
        string author,
        string content,
        bool isInternal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new IncidentComment
        {
            TenantId = tenantId,
            IncidentId = incidentId,
            Author = author.Trim(),
            Content = content.Trim(),
            IsInternal = isInternal
        };
    }

    public void Update(string content, bool isInternal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Content = content.Trim();
        IsInternal = isInternal;
        Touch();
    }

    public static IncidentComment FromPersistence(
        Guid id,
        Guid tenantId,
        Guid incidentId,
        string author,
        string content,
        bool isInternal,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            IncidentId = incidentId,
            Author = author,
            Content = content,
            IsInternal = isInternal,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

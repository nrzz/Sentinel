using Sentinel.Domain.Common;

namespace Sentinel.Domain.Incidents;

public enum IncidentTimelineEntryType
{
    Created = 0,
    StatusChanged = 1,
    SeverityChanged = 2,
    Assigned = 3,
    Comment = 4,
    AlertLinked = 5,
    System = 6
}

public sealed class IncidentTimelineEntry : Entity
{
    public Guid TenantId { get; private set; }
    public Guid IncidentId { get; private set; }
    public IncidentTimelineEntryType EntryType { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? Actor { get; private set; }
    public string? MetadataJson { get; private set; }

    private IncidentTimelineEntry() { }

    public static IncidentTimelineEntry Create(
        Guid tenantId,
        Guid incidentId,
        IncidentTimelineEntryType entryType,
        string message,
        string? actor = null,
        string? metadataJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new IncidentTimelineEntry
        {
            TenantId = tenantId,
            IncidentId = incidentId,
            EntryType = entryType,
            Message = message.Trim(),
            Actor = actor,
            MetadataJson = metadataJson
        };
    }

    public static IncidentTimelineEntry FromPersistence(
        Guid id,
        Guid tenantId,
        Guid incidentId,
        IncidentTimelineEntryType entryType,
        string message,
        string? actor,
        string? metadataJson,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            IncidentId = incidentId,
            EntryType = entryType,
            Message = message,
            Actor = actor,
            MetadataJson = metadataJson,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

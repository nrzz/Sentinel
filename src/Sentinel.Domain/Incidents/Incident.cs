using Sentinel.Domain.Common;

namespace Sentinel.Domain.Incidents;

public enum IncidentSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IncidentStatus
{
    Open = 0,
    Investigating = 1,
    Mitigated = 2,
    Resolved = 3,
    Closed = 4
}

public sealed class Incident : Entity
{
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }
    public Guid? SourceAlertExecutionId { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private Incident() { }

    public static Incident Create(
        Guid tenantId,
        string title,
        string description,
        IncidentSeverity severity,
        string? createdBy = null,
        Guid? sourceAlertExecutionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Incident
        {
            TenantId = tenantId,
            Title = title.Trim(),
            Description = description.Trim(),
            Severity = severity,
            Status = IncidentStatus.Open,
            CreatedBy = createdBy,
            SourceAlertExecutionId = sourceAlertExecutionId
        };
    }

    public void Update(string title, string description, IncidentSeverity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Description = description.Trim();
        Severity = severity;
        Touch();
    }

    public void Assign(string assignee)
    {
        AssignedTo = assignee.Trim();
        if (Status == IncidentStatus.Open)
        {
            Status = IncidentStatus.Investigating;
        }
        Touch();
    }

    public void SetStatus(IncidentStatus status)
    {
        Status = status;
        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            ResolvedAt = DateTimeOffset.UtcNow;
        }
        Touch();
    }

    public static Incident FromPersistence(
        Guid id,
        Guid tenantId,
        string title,
        string description,
        IncidentSeverity severity,
        IncidentStatus status,
        string? assignedTo,
        Guid? sourceAlertExecutionId,
        string? createdBy,
        DateTimeOffset? resolvedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Title = title,
            Description = description,
            Severity = severity,
            Status = status,
            AssignedTo = assignedTo,
            SourceAlertExecutionId = sourceAlertExecutionId,
            CreatedBy = createdBy,
            ResolvedAt = resolvedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

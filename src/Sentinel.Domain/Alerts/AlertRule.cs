using Sentinel.Domain.Common;

namespace Sentinel.Domain.Alerts;

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AlertRuleStatus
{
    Active = 0,
    Paused = 1,
    Disabled = 2
}

public sealed class AlertRule : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Query { get; private set; } = string.Empty;
    public string Condition { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public AlertRuleStatus Status { get; private set; }
    public TimeSpan EvaluationInterval { get; private set; }
    public IReadOnlyList<string> NotificationChannels { get; private set; } = [];
    public string? CreatedBy { get; private set; }

    private AlertRule() { }

    public static AlertRule Create(
        Guid tenantId,
        string name,
        string description,
        string query,
        string condition,
        AlertSeverity severity,
        TimeSpan evaluationInterval,
        IEnumerable<string> notificationChannels,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);

        return new AlertRule
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description.Trim(),
            Query = query.Trim(),
            Condition = condition.Trim(),
            Severity = severity,
            Status = AlertRuleStatus.Active,
            EvaluationInterval = evaluationInterval > TimeSpan.Zero
                ? evaluationInterval
                : TimeSpan.FromMinutes(5),
            NotificationChannels = notificationChannels.ToList().AsReadOnly(),
            CreatedBy = createdBy
        };
    }

    public void Update(
        string name,
        string description,
        string query,
        string condition,
        AlertSeverity severity,
        TimeSpan evaluationInterval,
        IEnumerable<string> notificationChannels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);

        Name = name.Trim();
        Description = description.Trim();
        Query = query.Trim();
        Condition = condition.Trim();
        Severity = severity;
        EvaluationInterval = evaluationInterval > TimeSpan.Zero
            ? evaluationInterval
            : EvaluationInterval;
        NotificationChannels = notificationChannels.ToList().AsReadOnly();
        Touch();
    }

    public void SetStatus(AlertRuleStatus status)
    {
        Status = status;
        Touch();
    }

    public static AlertRule FromPersistence(
        Guid id,
        Guid tenantId,
        string name,
        string description,
        string query,
        string condition,
        AlertSeverity severity,
        AlertRuleStatus status,
        TimeSpan evaluationInterval,
        IReadOnlyList<string> notificationChannels,
        string? createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Description = description,
            Query = query,
            Condition = condition,
            Severity = severity,
            Status = status,
            EvaluationInterval = evaluationInterval,
            NotificationChannels = notificationChannels,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

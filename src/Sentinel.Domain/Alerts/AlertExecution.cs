using Sentinel.Domain.Common;

namespace Sentinel.Domain.Alerts;

public enum AlertExecutionStatus
{
    Triggered = 0,
    Resolved = 1,
    Suppressed = 2,
    Failed = 3
}

public sealed class AlertExecution : Entity
{
    public Guid TenantId { get; private set; }
    public Guid AlertRuleId { get; private set; }
    public AlertExecutionStatus Status { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? MatchedValue { get; private set; }
    public DateTimeOffset TriggeredAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? CorrelationId { get; private set; }

    private AlertExecution() { }

    public static AlertExecution Create(
        Guid tenantId,
        Guid alertRuleId,
        AlertSeverity severity,
        string message,
        string? matchedValue = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new AlertExecution
        {
            TenantId = tenantId,
            AlertRuleId = alertRuleId,
            Status = AlertExecutionStatus.Triggered,
            Severity = severity,
            Message = message.Trim(),
            MatchedValue = matchedValue,
            TriggeredAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        };
    }

    public void Resolve()
    {
        Status = AlertExecutionStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Suppress(string reason)
    {
        Status = AlertExecutionStatus.Suppressed;
        Message = $"{Message} (suppressed: {reason})";
        Touch();
    }

    public void MarkFailed(string error)
    {
        Status = AlertExecutionStatus.Failed;
        Message = $"{Message} (failed: {error})";
        Touch();
    }

    public static AlertExecution FromPersistence(
        Guid id,
        Guid tenantId,
        Guid alertRuleId,
        AlertExecutionStatus status,
        AlertSeverity severity,
        string message,
        string? matchedValue,
        DateTimeOffset triggeredAt,
        DateTimeOffset? resolvedAt,
        string? correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            AlertRuleId = alertRuleId,
            Status = status,
            Severity = severity,
            Message = message,
            MatchedValue = matchedValue,
            TriggeredAt = triggeredAt,
            ResolvedAt = resolvedAt,
            CorrelationId = correlationId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

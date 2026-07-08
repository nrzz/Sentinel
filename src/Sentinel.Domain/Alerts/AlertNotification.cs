using Sentinel.Domain.Common;

namespace Sentinel.Domain.Alerts;

public enum AlertNotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3
}

public sealed class AlertNotification : Entity
{
    public Guid TenantId { get; private set; }
    public Guid AlertExecutionId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public AlertNotificationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AlertNotification() { }

    public static AlertNotification Create(
        Guid tenantId,
        Guid alertExecutionId,
        string channel,
        string recipient,
        string subject,
        string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);

        return new AlertNotification
        {
            TenantId = tenantId,
            AlertExecutionId = alertExecutionId,
            Channel = channel.Trim(),
            Recipient = recipient.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = AlertNotificationStatus.Pending
        };
    }

    public void MarkSent()
    {
        Status = AlertNotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkFailed(string error)
    {
        Status = AlertNotificationStatus.Failed;
        ErrorMessage = error;
        Touch();
    }

    public void MarkSkipped(string reason)
    {
        Status = AlertNotificationStatus.Skipped;
        ErrorMessage = reason;
        Touch();
    }
}

namespace Sentinel.PluginSdk;

public sealed record AlertNotification(
    Guid TenantId,
    Guid AlertRuleId,
    Guid? ExecutionId,
    string RuleName,
    string Severity,
    string Message,
    string? MatchedValue,
    DateTimeOffset TriggeredAt,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AlertDeliveryResult(
    bool Success,
    string ChannelMessageId,
    string? Error = null);

/// <summary>Delivers alert notifications to external channels.</summary>
public interface IAlertChannelPlugin : IPluginMetadata
{
    Task<AlertDeliveryResult> SendAsync(
        AlertNotification notification,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default);
}

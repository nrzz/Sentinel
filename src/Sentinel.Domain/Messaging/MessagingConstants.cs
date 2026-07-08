namespace Sentinel.Domain.Messaging;

public static class MessagingConstants
{
    public const string LogsExchange = "sentinel.logs";
    public const string MetricsExchange = "sentinel.metrics";
    public const string TracesExchange = "sentinel.traces";
    public const string AlertsExchange = "sentinel.alerts";
    public const string NotificationsExchange = "sentinel.notifications";

    public const string LogReceivedRoutingKey = "log.received";
    public const string LogEnrichedRoutingKey = "log.enriched";
    public const string MetricReceivedRoutingKey = "metric.received";
    public const string TraceReceivedRoutingKey = "trace.received";
    public const string AlertTriggeredRoutingKey = "alert.triggered";
    public const string NotificationSendRoutingKey = "notification.send";
    public const string DeadLetterRoutingKey = "dead.letter";

    public const string CollectorQueue = "collector";
    public const string EnrichmentQueue = "enrichment";
    public const string PersistenceQueue = "persistence";
}

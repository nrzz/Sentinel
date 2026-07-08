namespace Sentinel.Sdk.Models;

public sealed record LoginRequest(string Email, string Password, Guid? TenantId = null);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthUser(
    Guid Id,
    string Email,
    string DisplayName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthUser User);

public sealed record LogEntryInput(
    string Service,
    string Environment,
    string Level,
    string Message,
    Dictionary<string, string>? Attributes = null,
    string? TraceId = null,
    string? SpanId = null,
    string? CorrelationId = null,
    DateTimeOffset? Timestamp = null);

public sealed record IngestLogsRequest(IReadOnlyList<LogEntryInput> Logs);

public sealed record IngestLogsResponse(int AcceptedCount, string Status);

public sealed record LogSearchQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Level = null,
    string? Service = null,
    string? Environment = null,
    string? Query = null,
    string? TraceId = null,
    int Limit = 100,
    int Offset = 0);

public sealed record LogSearchItem(
    Guid Id,
    DateTimeOffset Timestamp,
    string Service,
    string Environment,
    string Level,
    string NormalizedLevel,
    string Message,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    string? ParsedException,
    string? SourceHost);

public sealed record SearchLogsResponse(
    IReadOnlyList<LogSearchItem> Items,
    long TotalCount,
    int Limit,
    int Offset);

public sealed record MetricEntryInput(
    string Name,
    double Value,
    string Unit,
    Dictionary<string, string>? Tags,
    string Service,
    string Environment,
    DateTimeOffset? Timestamp = null);

public sealed record IngestMetricsRequest(IReadOnlyList<MetricEntryInput> Metrics);

public sealed record IngestMetricsResponse(int AcceptedCount, string Status);

public sealed record MetricQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Name = null,
    string? Service = null,
    string? Environment = null,
    int Limit = 1000);

public sealed record MetricItem(
    Guid Id,
    DateTimeOffset Timestamp,
    string Name,
    double Value,
    string Unit,
    string Service,
    string Environment,
    Dictionary<string, string> Tags);

public sealed record QueryMetricsResponse(IReadOnlyList<MetricItem> Items);

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertRuleStatus
{
    Active,
    Paused,
    Disabled
}

public sealed record AlertRule(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    string Query,
    string Condition,
    AlertSeverity Severity,
    AlertRuleStatus Status,
    long EvaluationIntervalSeconds,
    IReadOnlyList<string> NotificationChannels,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public enum AlertExecutionStatus
{
    Triggered,
    Resolved,
    Suppressed
}

public sealed record AlertExecution(
    Guid Id,
    Guid AlertRuleId,
    AlertExecutionStatus Status,
    AlertSeverity Severity,
    string Message,
    string? MatchedValue,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ResolvedAt,
    string? CorrelationId,
    DateTimeOffset CreatedAt);

public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    Open,
    Investigating,
    Mitigated,
    Resolved,
    Closed
}

public sealed record Incident(
    Guid Id,
    string Title,
    string Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    string? AssignedTo,
    Guid? SourceAlertExecutionId,
    string? CreatedBy,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record Tenant(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    string Environment,
    string SettingsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class SentinelApiException : Exception
{
    public SentinelApiException(int statusCode, string message, string? responseBody = null)
        : base($"Sentinel API error {statusCode}: {message}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }
    public string? ResponseBody { get; }
}

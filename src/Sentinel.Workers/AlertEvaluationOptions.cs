namespace Sentinel.Workers;

public sealed class AlertRuleOptions
{
    public const string SectionName = "AlertRules";

    public string Name { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public string Operator { get; set; } = "GreaterThan";
    public string Severity { get; set; } = "warning";
    public Guid? TenantId { get; set; }
}

public sealed class AlertEvaluationOptions
{
    public const string SectionName = "AlertEvaluation";

    public List<AlertRuleOptions> Rules { get; set; } = [];
    public int ErrorLogThresholdPerMinute { get; set; } = 100;
}

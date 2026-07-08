using System.Net.Http.Json;
using Sentinel.PluginSdk;

namespace Sentinel.Plugins.SlackAlert;

public sealed class SlackAlertPlugin : PluginBase, IAlertChannelPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public override string Id => "sentinel.plugins.slack-alert";
    public override string Name => "Slack Alert Channel";
    public override string Version => "1.0.0";
    public override string Description => "Sends Sentinel alert notifications to a Slack incoming webhook.";
    public override string Author => "Sentinel";
    public override IReadOnlyList<string> Tags => ["alerts", "slack", "notifications"];

    public override IReadOnlyDictionary<string, ConfigSchemaProperty> GetConfigSchema() =>
        new Dictionary<string, ConfigSchemaProperty>
        {
            ["webhookUrl"] = new("webhookUrl", "string", "Slack incoming webhook URL.", true),
            ["channel"] = new("channel", "string", "Optional Slack channel override.", false),
            ["username"] = new("username", "string", "Bot display name.", false, "Sentinel"),
            ["iconEmoji"] = new("iconEmoji", "string", "Bot icon emoji.", false, ":rotating_light:")
        };

    public override PluginHealthResult CheckHealth()
    {
        if (!Settings.TryGetValue("webhookUrl", out var url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return new PluginHealthResult(PluginHealthStatus.Unhealthy, "webhookUrl is not configured.");
        }

        return new PluginHealthResult(PluginHealthStatus.Healthy, "Slack webhook URL is configured.");
    }

    public async Task<AlertDeliveryResult> SendAsync(
        AlertNotification notification,
        IReadOnlyDictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.TryGetValue("webhookUrl", out var webhookUrl) || string.IsNullOrWhiteSpace(webhookUrl))
        {
            return new AlertDeliveryResult(false, string.Empty, "webhookUrl setting is required.");
        }

        var username = settings.GetValueOrDefault("username", "Sentinel");
        var iconEmoji = settings.GetValueOrDefault("iconEmoji", ":rotating_light:");
        var channel = settings.GetValueOrDefault("channel");

        var payload = new SlackMessagePayload
        {
            Username = username,
            IconEmoji = iconEmoji,
            Channel = string.IsNullOrWhiteSpace(channel) ? null : channel,
            Text = $"*{notification.Severity.ToUpperInvariant()}* — {notification.RuleName}",
            Attachments =
            [
                new SlackAttachment
                {
                    Color = MapSeverityColor(notification.Severity),
                    Fields =
                    [
                        new SlackField { Title = "Message", Value = notification.Message, Short = false },
                        new SlackField { Title = "Triggered At", Value = notification.TriggeredAt.ToString("O"), Short = true },
                        new SlackField { Title = "Rule ID", Value = notification.AlertRuleId.ToString(), Short = true }
                    ]
                }
            ]
        };

        if (!string.IsNullOrWhiteSpace(notification.MatchedValue))
        {
            payload.Attachments[0].Fields.Add(new SlackField
            {
                Title = "Matched Value",
                Value = notification.MatchedValue,
                Short = true
            });
        }

        using var response = await HttpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AlertDeliveryResult(false, string.Empty, $"Slack API returned {(int)response.StatusCode}: {body}");
        }

        var messageId = response.Headers.TryGetValues("X-Slack-Unique-Id", out var ids)
            ? ids.FirstOrDefault() ?? Guid.NewGuid().ToString("N")
            : Guid.NewGuid().ToString("N");

        return new AlertDeliveryResult(true, messageId);
    }

    private static string MapSeverityColor(string severity) =>
        severity.ToLowerInvariant() switch
        {
            "critical" => "#d32f2f",
            "warning" => "#f9a825",
            _ => "#1976d2"
        };

    private sealed class SlackMessagePayload
    {
        public string? Channel { get; set; }
        public string Username { get; set; } = "Sentinel";
        public string IconEmoji { get; set; } = ":rotating_light:";
        public string Text { get; set; } = string.Empty;
        public List<SlackAttachment> Attachments { get; set; } = [];
    }

    private sealed class SlackAttachment
    {
        public string Color { get; set; } = "#1976d2";
        public List<SlackField> Fields { get; set; } = [];
    }

    private sealed class SlackField
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Short { get; set; }
    }
}

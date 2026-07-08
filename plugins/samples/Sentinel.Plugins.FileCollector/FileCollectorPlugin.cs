using System.Text.RegularExpressions;
using Sentinel.PluginSdk;

namespace Sentinel.Plugins.FileCollector;

public sealed partial class FileCollectorPlugin : PluginBase, ICollectorPlugin
{
    [GeneratedRegex(@"^(?<timestamp>\S+)\s+(?<level>\S+)\s+(?<message>.*)$", RegexOptions.Compiled)]
    private static partial Regex DefaultLinePattern();

    public override string Id => "sentinel.plugins.file-collector";
    public override string Name => "File Log Collector";
    public override string Version => "1.0.0";
    public override string Description => "Collects log lines from local files and ingests them into Sentinel.";
    public override string Author => "Sentinel";
    public override IReadOnlyList<string> Tags => ["collector", "files", "logs"];

    public override IReadOnlyDictionary<string, ConfigSchemaProperty> GetConfigSchema() =>
        new Dictionary<string, ConfigSchemaProperty>
        {
            ["path"] = new("path", "string", "Absolute or relative path to the log file.", true),
            ["service"] = new("service", "string", "Service name applied to collected entries.", true),
            ["environment"] = new("environment", "string", "Environment label.", false, "production"),
            ["defaultLevel"] = new("defaultLevel", "string", "Fallback log level when parsing fails.", false, "info"),
            ["tailBytes"] = new("tailBytes", "number", "Maximum bytes to read from the end of the file.", false, 65536)
        };

    public override PluginHealthResult CheckHealth()
    {
        if (!Settings.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new PluginHealthResult(PluginHealthStatus.Unhealthy, "path is not configured.");
        }

        if (!File.Exists(path))
        {
            return new PluginHealthResult(PluginHealthStatus.Degraded, $"Log file not found: {path}");
        }

        return new PluginHealthResult(PluginHealthStatus.Healthy, "Log file is accessible.");
    }

    public Task<CollectorResult> CollectAsync(CollectorContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Settings.GetValueOrDefault("path")
            ?? Settings.GetValueOrDefault("path")
            ?? throw new InvalidOperationException("path setting is required.");

        if (!File.Exists(path))
        {
            return Task.FromResult(new CollectorResult([], false, null));
        }

        var service = context.Settings.GetValueOrDefault("service", Settings.GetValueOrDefault("service", "unknown"));
        var environment = context.Settings.GetValueOrDefault("environment", Settings.GetValueOrDefault("environment", "production"));
        var defaultLevel = context.Settings.GetValueOrDefault("defaultLevel", Settings.GetValueOrDefault("defaultLevel", "info"));
        var tailBytes = int.TryParse(context.Settings.GetValueOrDefault("tailBytes", Settings.GetValueOrDefault("tailBytes", "65536")), out var parsedTail)
            ? parsedTail
            : 65536;

        var lines = ReadTailLines(path, tailBytes, context.MaxEntries, context.Since);
        var entries = lines.Select(line => ParseLine(line, service, environment, defaultLevel, path)).ToList();

        return Task.FromResult(new CollectorResult(entries, false, DateTimeOffset.UtcNow));
    }

    private static IReadOnlyList<string> ReadTailLines(string path, int tailBytes, int maxEntries, DateTimeOffset? since)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var length = stream.Length;
        var readSize = (int)Math.Min(tailBytes, length);
        stream.Seek(-readSize, SeekOrigin.End);

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IEnumerable<string> filtered = lines;
        if (since.HasValue)
        {
            filtered = lines.Where(line =>
            {
                var match = DefaultLinePattern().Match(line);
                if (!match.Success)
                {
                    return true;
                }

                return DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var ts) && ts >= since.Value;
            });
        }

        return filtered.TakeLast(maxEntries).ToList();
    }

    private static CollectedLogEntry ParseLine(string line, string service, string environment, string defaultLevel, string sourcePath)
    {
        var match = DefaultLinePattern().Match(line);
        if (match.Success
            && DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var timestamp))
        {
            return new CollectedLogEntry(
                service,
                environment,
                match.Groups["level"].Value.ToLowerInvariant(),
                match.Groups["message"].Value,
                timestamp,
                SourcePath: sourcePath);
        }

        return new CollectedLogEntry(
            service,
            environment,
            defaultLevel,
            line,
            DateTimeOffset.UtcNow,
            SourcePath: sourcePath);
    }
}

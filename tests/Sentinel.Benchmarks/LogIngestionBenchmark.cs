using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Sentinel.Api.Features.Logs;
using Sentinel.Domain.Events;

namespace Sentinel.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class LogIngestionBenchmark
{
    private IngestLogsRequest _request = null!;
    private List<LogReceived> _events = null!;

    [GlobalSetup]
    public void Setup()
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var receivedAt = DateTimeOffset.UtcNow;

        _request = new IngestLogsRequest(
            Logs: Enumerable.Range(0, 50).Select(i => new LogEntryDto(
                Service: "benchmark-service",
                Environment: "benchmark",
                Level: "info",
                Message: $"Benchmark log message {i}",
                Attributes: new Dictionary<string, string> { ["index"] = i.ToString() },
                TraceId: $"trace-{i}",
                SpanId: $"span-{i}",
                CorrelationId: $"corr-{i}",
                Timestamp: receivedAt)).ToList());

        _events = _request.Logs.Select(log => new LogReceived(
            Id: Guid.NewGuid(),
            TenantId: tenantId,
            Timestamp: log.Timestamp ?? receivedAt,
            Service: log.Service,
            Environment: log.Environment,
            Level: log.Level,
            Message: log.Message,
            Attributes: log.Attributes ?? new Dictionary<string, string>(),
            TraceId: log.TraceId,
            SpanId: log.SpanId,
            CorrelationId: log.CorrelationId ?? Guid.NewGuid().ToString(),
            ReceivedAt: receivedAt)).ToList();
    }

    [Benchmark]
    public int MapLogsToEvents()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var events = _request.Logs.Select(log => new LogReceived(
            Id: Guid.NewGuid(),
            TenantId: tenantId,
            Timestamp: log.Timestamp ?? receivedAt,
            Service: log.Service,
            Environment: log.Environment,
            Level: log.Level,
            Message: log.Message,
            Attributes: log.Attributes ?? new Dictionary<string, string>(),
            TraceId: log.TraceId,
            SpanId: log.SpanId,
            CorrelationId: log.CorrelationId ?? Guid.NewGuid().ToString(),
            ReceivedAt: receivedAt)).ToList();

        return events.Count;
    }

    [Benchmark]
    public int SerializeLogEvents()
    {
        var count = 0;
        foreach (var logEvent in _events)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(logEvent);
            count += json.Length;
        }

        return count;
    }

    [Benchmark]
    public int BuildBatchPayload()
    {
        var payload = new { logs = _request.Logs };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return json.Length;
    }
}

public static class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run<LogIngestionBenchmark>();
}

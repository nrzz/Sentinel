using Grpc.Core;
using Sentinel.Api.Grpc;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Api.Features.Traces;

public sealed class TraceIngestionGrpcService : TraceIngestion.TraceIngestionBase
{
    private readonly IRabbitMqPublisher _publisher;

    public TraceIngestionGrpcService(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async Task<IngestResponse> StreamTraces(
        IAsyncStreamReader<TraceMessage> requestStream,
        ServerCallContext context)
    {
        var tenantId = ResolveTenantId(context);
        var accepted = 0;
        var receivedAt = DateTimeOffset.UtcNow;

        await foreach (var entry in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var spans = entry.Spans.Select(span => new TraceSpanReceived(
                SpanId: span.SpanId,
                ParentSpanId: string.IsNullOrEmpty(span.ParentSpanId) ? null : span.ParentSpanId,
                Timestamp: span.TimestampUnixMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(span.TimestampUnixMs)
                    : receivedAt,
                Name: span.Name,
                Service: span.Service,
                DurationMs: span.DurationMs,
                Status: span.Status,
                Attributes: span.Attributes.ToDictionary(static pair => pair.Key, static pair => pair.Value))).ToList();

            var traceEvent = new TraceReceived(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                TraceId: entry.TraceId,
                Timestamp: entry.TimestampUnixMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(entry.TimestampUnixMs)
                    : receivedAt,
                Service: entry.Service,
                Name: entry.Name,
                DurationMs: entry.DurationMs,
                Status: entry.Status,
                Attributes: entry.Attributes.ToDictionary(static pair => pair.Key, static pair => pair.Value),
                Spans: spans,
                ReceivedAt: receivedAt);

            await _publisher.PublishAsync(
                MessagingConstants.TracesExchange,
                MessagingConstants.TraceReceivedRoutingKey,
                traceEvent,
                context.CancellationToken);

            accepted++;
        }

        return new IngestResponse { AcceptedCount = accepted, Status = "accepted" };
    }

    private static Guid ResolveTenantId(ServerCallContext context)
    {
        var tenantHeader = context.RequestHeaders.FirstOrDefault(h =>
            h.Key.Equals("x-tenant-id", StringComparison.OrdinalIgnoreCase));

        if (tenantHeader is not null && Guid.TryParse(tenantHeader.Value, out var tenantId))
        {
            return tenantId;
        }

        throw new RpcException(new Status(StatusCode.Unauthenticated, "A valid X-Tenant-ID header is required."));
    }
}

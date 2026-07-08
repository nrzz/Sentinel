using Grpc.Core;
using Sentinel.Api.Grpc;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Api.Features.Logs;

public sealed class LogIngestionGrpcService : LogIngestion.LogIngestionBase
{
    private readonly IRabbitMqPublisher _publisher;

    public LogIngestionGrpcService(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async Task<IngestResponse> StreamLogs(
        IAsyncStreamReader<LogEntryMessage> requestStream,
        ServerCallContext context)
    {
        var tenantId = ResolveTenantId(context);
        var accepted = 0;
        var receivedAt = DateTimeOffset.UtcNow;

        await foreach (var entry in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var logEvent = new LogReceived(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                Timestamp: entry.TimestampUnixMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(entry.TimestampUnixMs)
                    : receivedAt,
                Service: entry.Service,
                Environment: entry.Environment,
                Level: entry.Level,
                Message: entry.Message,
                Attributes: entry.Attributes.ToDictionary(static pair => pair.Key, static pair => pair.Value),
                TraceId: string.IsNullOrEmpty(entry.TraceId) ? null : entry.TraceId,
                SpanId: string.IsNullOrEmpty(entry.SpanId) ? null : entry.SpanId,
                CorrelationId: string.IsNullOrEmpty(entry.CorrelationId) ? null : entry.CorrelationId,
                ReceivedAt: receivedAt);

            await _publisher.PublishAsync(
                MessagingConstants.LogsExchange,
                MessagingConstants.LogReceivedRoutingKey,
                logEvent,
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

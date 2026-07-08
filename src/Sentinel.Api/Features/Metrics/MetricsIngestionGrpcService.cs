using Grpc.Core;
using Sentinel.Api.Grpc;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Api.Features.Metrics;

public sealed class MetricsIngestionGrpcService : MetricsIngestion.MetricsIngestionBase
{
    private readonly IRabbitMqPublisher _publisher;

    public MetricsIngestionGrpcService(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async Task<IngestResponse> StreamMetrics(
        IAsyncStreamReader<MetricMessage> requestStream,
        ServerCallContext context)
    {
        var tenantId = ResolveTenantId(context);
        var accepted = 0;
        var receivedAt = DateTimeOffset.UtcNow;

        await foreach (var entry in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var metricEvent = new MetricReceived(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                Timestamp: entry.TimestampUnixMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(entry.TimestampUnixMs)
                    : receivedAt,
                Name: entry.Name,
                Value: entry.Value,
                Unit: entry.Unit,
                Tags: entry.Tags.ToDictionary(static pair => pair.Key, static pair => pair.Value),
                Service: entry.Service,
                Environment: entry.Environment,
                ReceivedAt: receivedAt);

            await _publisher.PublishAsync(
                MessagingConstants.MetricsExchange,
                MessagingConstants.MetricReceivedRoutingKey,
                metricEvent,
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

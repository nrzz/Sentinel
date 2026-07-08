using FluentValidation;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Common;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Domain.Observability;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Api.Features.Metrics;

public static class IngestMetricsEndpoint
{
    public static void MapIngestMetricsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/metrics", HandleAsync)
            .WithName("IngestMetrics")
            .WithTags("Metrics")
            .Produces<IngestMetricsResponse>(StatusCodes.Status202Accepted)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] IngestMetricsRequest request,
        HttpContext httpContext,
        IValidator<IngestMetricsRequest> validator,
        IRabbitMqPublisher publisher,
        IMetricRepository metricRepository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        Guid tenantId;
        try
        {
            tenantId = TenantResolver.ResolveTenantId(httpContext);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var records = new List<MetricRecord>(request.Metrics.Count);

        foreach (var metric in request.Metrics)
        {
            var metricId = Guid.NewGuid();
            var metricEvent = new MetricReceived(
                Id: metricId,
                TenantId: tenantId,
                Timestamp: metric.Timestamp ?? receivedAt,
                Name: metric.Name,
                Value: metric.Value,
                Unit: metric.Unit,
                Tags: metric.Tags ?? new Dictionary<string, string>(),
                Service: metric.Service,
                Environment: metric.Environment,
                ReceivedAt: receivedAt);

            await publisher.PublishAsync(
                MessagingConstants.MetricsExchange,
                MessagingConstants.MetricReceivedRoutingKey,
                metricEvent,
                cancellationToken);

            records.Add(new MetricRecord(
                metricId,
                tenantId,
                metricEvent.Timestamp,
                metric.Name,
                metric.Value,
                metric.Unit,
                JsonSerializer.Serialize(metricEvent.Tags),
                metric.Service,
                metric.Environment));
        }

        await metricRepository.BatchInsertAsync(records, cancellationToken);

        return Results.Accepted(value: new IngestMetricsResponse(request.Metrics.Count, "accepted"));
    }
}

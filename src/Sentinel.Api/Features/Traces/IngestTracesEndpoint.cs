using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Common;
using Sentinel.Api.Features.Ingestion;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Domain.Observability;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Api.Features.Traces;

public static class IngestTracesEndpoint
{
    public static void MapIngestTracesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/traces", HandleAsync)
            .WithName("IngestTraces")
            .WithTags("Traces")
            .Produces<IngestTracesResponse>(StatusCodes.Status202Accepted)
            .AllowAnonymous()
            .WithIngestionAuth();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] IngestTracesRequest request,
        HttpContext httpContext,
        IValidator<IngestTracesRequest> validator,
        IRabbitMqPublisher publisher,
        ITraceRepository traceRepository,
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
        var traceRecords = new List<TraceRecord>(request.Traces.Count);
        var spanRecords = new List<TraceSpanRecord>();

        foreach (var trace in request.Traces)
        {
            var traceId = Guid.NewGuid();
            var spans = trace.Spans?.Select(span => new TraceSpanReceived(
                SpanId: span.SpanId,
                ParentSpanId: span.ParentSpanId,
                Timestamp: span.Timestamp ?? trace.Timestamp ?? receivedAt,
                Name: span.Name,
                Service: span.Service,
                DurationMs: span.DurationMs,
                Status: span.Status,
                Attributes: span.Attributes ?? new Dictionary<string, string>())).ToList()
                ?? [];

            var traceEvent = new TraceReceived(
                Id: traceId,
                TenantId: tenantId,
                TraceId: trace.TraceId,
                Timestamp: trace.Timestamp ?? receivedAt,
                Service: trace.Service,
                Name: trace.Name,
                DurationMs: trace.DurationMs,
                Status: trace.Status,
                Attributes: trace.Attributes ?? new Dictionary<string, string>(),
                Spans: spans,
                ReceivedAt: receivedAt);

            await publisher.PublishAsync(
                MessagingConstants.TracesExchange,
                MessagingConstants.TraceReceivedRoutingKey,
                traceEvent,
                cancellationToken);

            traceRecords.Add(new TraceRecord(
                traceId,
                tenantId,
                trace.TraceId,
                traceEvent.Timestamp,
                trace.Service,
                trace.Name,
                trace.DurationMs,
                trace.Status,
                JsonSerializer.Serialize(traceEvent.Attributes)));

            spanRecords.AddRange(spans.Select(span => new TraceSpanRecord(
                Guid.NewGuid(),
                tenantId,
                trace.TraceId,
                span.SpanId,
                span.ParentSpanId,
                span.Timestamp,
                span.Name,
                span.Service,
                span.DurationMs,
                span.Status,
                JsonSerializer.Serialize(span.Attributes))));
        }

        await traceRepository.BatchInsertTracesAsync(traceRecords, cancellationToken);
        if (spanRecords.Count > 0)
        {
            await traceRepository.BatchInsertSpansAsync(spanRecords, cancellationToken);
        }

        return Results.Accepted(value: new IngestTracesResponse(request.Traces.Count, "accepted"));
    }
}

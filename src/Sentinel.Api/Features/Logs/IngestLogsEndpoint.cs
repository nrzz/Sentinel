using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Sentinel.Api.Common;
using Sentinel.Api.Hubs;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Infrastructure.Messaging;

namespace Sentinel.Api.Features.Logs;

public static class IngestLogsEndpoint
{
    public static void MapIngestLogsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/logs", HandleAsync)
            .WithName("IngestLogs")
            .WithTags("Logs")
            .Produces<IngestLogsResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] IngestLogsRequest request,
        HttpContext httpContext,
        IValidator<IngestLogsRequest> validator,
        IRabbitMqPublisher publisher,
        IHubContext<LogsHub> logsHub,
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
        var events = request.Logs.Select(log => new LogReceived(
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
            CorrelationId: log.CorrelationId ?? httpContext.TraceIdentifier,
            ReceivedAt: receivedAt)).ToList();

        foreach (var logEvent in events)
        {
            await publisher.PublishAsync(
                MessagingConstants.LogsExchange,
                MessagingConstants.LogReceivedRoutingKey,
                logEvent,
                cancellationToken);

            await logsHub.Clients.Group(tenantId.ToString()).SendAsync(
                "LogReceived",
                new LogStreamDto(
                    logEvent.Id,
                    logEvent.TenantId,
                    logEvent.Timestamp,
                    logEvent.Service,
                    logEvent.Environment,
                    logEvent.Level,
                    logEvent.Message,
                    logEvent.TraceId,
                    logEvent.CorrelationId),
                cancellationToken);
        }

        return Results.Accepted(value: new IngestLogsResponse(events.Count, "accepted"));
    }
}

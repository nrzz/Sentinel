using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Common;
using Sentinel.Domain.Events;
using Sentinel.Domain.Messaging;
using Sentinel.Domain.Observability;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;
using System.Text.Json;

namespace Sentinel.Api.Features.Deployments;

public static class DeploymentWebhookEndpoint
{
    public static void MapDeploymentWebhookEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/deployments/webhook", HandleAsync)
            .WithName("DeploymentWebhook")
            .WithTags("Deployments")
            .Produces<DeploymentWebhookResponse>(StatusCodes.Status202Accepted)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] DeploymentWebhookRequest request,
        HttpContext httpContext,
        IValidator<DeploymentWebhookRequest> validator,
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
        var deploymentId = Guid.NewGuid();
        var metadata = request.Metadata ?? new Dictionary<string, string>();

        var deploymentEvent = new DeploymentReceived(
            Id: deploymentId,
            TenantId: tenantId,
            Timestamp: request.Timestamp ?? receivedAt,
            Service: request.Service,
            Version: request.Version,
            Environment: request.Environment,
            Status: request.Status,
            CommitSha: request.CommitSha,
            Repository: request.Repository,
            Metadata: metadata,
            ReceivedAt: receivedAt);

        await publisher.PublishAsync(
            MessagingConstants.LogsExchange,
            "deployment.received",
            deploymentEvent,
            cancellationToken);

        var deploymentRecord = new DeploymentRecord(
            Id: deploymentId,
            TenantId: tenantId,
            Timestamp: deploymentEvent.Timestamp,
            Service: request.Service,
            Version: request.Version,
            Environment: request.Environment,
            Status: request.Status,
            CommitSha: request.CommitSha,
            Repository: request.Repository,
            MetadataJson: JsonSerializer.Serialize(metadata));

        await traceRepository.BatchInsertDeploymentsAsync([deploymentRecord], cancellationToken);

        return Results.Accepted(value: new DeploymentWebhookResponse(deploymentId, "accepted"));
    }
}

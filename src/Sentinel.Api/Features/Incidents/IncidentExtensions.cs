using Microsoft.AspNetCore.Mvc;
using Sentinel.Api;
using Sentinel.Api.Authorization;
using Sentinel.Domain.Incidents;
using Sentinel.Infrastructure.Incidents;

namespace Sentinel.Api.Features.Incidents;

public static class IncidentExtensions
{
    public static IServiceCollection AddIncidentFeatures(this IServiceCollection services)
    {
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidents")
            .WithTags("Incidents")
            .RequireAuthorization();

        group.MapGet("/", ListIncidents).RequireAuthorization(SentinelPolicies.IncidentsRead);
        group.MapGet("/{id:guid}", GetIncident).RequireAuthorization(SentinelPolicies.IncidentsRead);
        group.MapPost("/", CreateIncident).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapPut("/{id:guid}", UpdateIncident).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapPatch("/{id:guid}", PatchIncident).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapDelete("/{id:guid}", DeleteIncident).RequireAuthorization(SentinelPolicies.IncidentsDelete);
        group.MapGet("/{id:guid}/timeline", ListTimeline).RequireAuthorization(SentinelPolicies.IncidentsRead);
        group.MapPost("/{id:guid}/timeline", AddTimelineEntry).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapGet("/{id:guid}/comments", ListComments).RequireAuthorization(SentinelPolicies.IncidentsRead);
        group.MapPost("/{id:guid}/comments", AddComment).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapPut("/{id:guid}/comments/{commentId:guid}", UpdateComment).RequireAuthorization(SentinelPolicies.IncidentsWrite);
        group.MapDelete("/{id:guid}/comments/{commentId:guid}", DeleteComment).RequireAuthorization(SentinelPolicies.IncidentsDelete);

        return app;
    }

    private static async Task<IResult> ListIncidents(
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incidents = await repository.ListAsync(tenantId, cancellationToken);
        return Results.Ok(incidents.Select(IncidentResponse.FromEntity));
    }

    private static async Task<IResult> GetIncident(
        Guid id,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        return incident is null ? Results.NotFound() : Results.Ok(IncidentResponse.FromEntity(incident));
    }

    private static async Task<IResult> CreateIncident(
        CreateIncidentRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = Incident.Create(
            tenantId,
            request.Title,
            request.Description,
            request.Severity,
            context.GetUserId(),
            request.SourceAlertExecutionId);

        await repository.CreateAsync(incident, cancellationToken);

        var timelineEntry = IncidentTimelineEntry.Create(
            tenantId,
            incident.Id,
            IncidentTimelineEntryType.Created,
            $"Incident created: {incident.Title}",
            context.GetUserId());

        await repository.AddTimelineEntryAsync(timelineEntry, cancellationToken);

        return Results.Created($"/api/v1/incidents/{incident.Id}", IncidentResponse.FromEntity(incident));
    }

    private static async Task<IResult> UpdateIncident(
        Guid id,
        UpdateIncidentRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var previousStatus = incident.Status;
        incident.Update(request.Title, request.Description, request.Severity);

        if (!string.IsNullOrWhiteSpace(request.AssignedTo))
        {
            incident.Assign(request.AssignedTo);
        }

        if (request.Status.HasValue)
        {
            incident.SetStatus(request.Status.Value);
        }

        await repository.UpdateAsync(incident, cancellationToken);

        if (request.Status.HasValue && request.Status.Value != previousStatus)
        {
            var entry = IncidentTimelineEntry.Create(
                tenantId,
                incident.Id,
                IncidentTimelineEntryType.StatusChanged,
                $"Status changed from {previousStatus} to {request.Status.Value}",
                context.GetUserId());

            await repository.AddTimelineEntryAsync(entry, cancellationToken);
        }

        return Results.Ok(IncidentResponse.FromEntity(incident));
    }

    private static async Task<IResult> PatchIncident(
        Guid id,
        PatchIncidentRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var previousStatus = incident.Status;

        if (request.Status.HasValue)
        {
            incident.SetStatus(request.Status.Value);
        }

        if (request.AssignedTo is not null)
        {
            incident.Assign(request.AssignedTo);
        }

        await repository.UpdateAsync(incident, cancellationToken);

        if (request.Status.HasValue && request.Status.Value != previousStatus)
        {
            var entry = IncidentTimelineEntry.Create(
                tenantId,
                incident.Id,
                IncidentTimelineEntryType.StatusChanged,
                $"Status changed from {previousStatus} to {request.Status.Value}",
                context.GetUserId());

            await repository.AddTimelineEntryAsync(entry, cancellationToken);
        }

        return Results.Ok(IncidentResponse.FromEntity(incident));
    }

    private static async Task<IResult> DeleteIncident(
        Guid id,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        await repository.DeleteAsync(tenantId, id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListTimeline(
        Guid id,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var entries = await repository.ListTimelineAsync(tenantId, id, cancellationToken);
        return Results.Ok(entries.Select(TimelineEntryResponse.FromEntity));
    }

    private static async Task<IResult> AddTimelineEntry(
        Guid id,
        AddTimelineEntryRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var entry = IncidentTimelineEntry.Create(
            tenantId,
            id,
            request.EntryType,
            request.Message,
            context.GetUserId(),
            request.MetadataJson);

        await repository.AddTimelineEntryAsync(entry, cancellationToken);
        return Results.Created($"/api/v1/incidents/{id}/timeline", TimelineEntryResponse.FromEntity(entry));
    }

    private static async Task<IResult> ListComments(
        Guid id,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var comments = await repository.ListCommentsAsync(tenantId, id, cancellationToken);
        return Results.Ok(comments.Select(CommentResponse.FromEntity));
    }

    private static async Task<IResult> AddComment(
        Guid id,
        AddCommentRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var incident = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        var author = context.GetUserId() ?? request.Author;
        var comment = IncidentComment.Create(tenantId, id, author, request.Content, request.IsInternal);
        await repository.AddCommentAsync(comment, cancellationToken);

        var timelineEntry = IncidentTimelineEntry.Create(
            tenantId,
            id,
            IncidentTimelineEntryType.Comment,
            $"Comment added by {author}",
            author);

        await repository.AddTimelineEntryAsync(timelineEntry, cancellationToken);

        return Results.Created($"/api/v1/incidents/{id}/comments/{comment.Id}", CommentResponse.FromEntity(comment));
    }

    private static async Task<IResult> UpdateComment(
        Guid id,
        Guid commentId,
        UpdateCommentRequest request,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var comment = await repository.GetCommentByIdAsync(tenantId, commentId, cancellationToken);
        if (comment is null || comment.IncidentId != id)
        {
            return Results.NotFound();
        }

        comment.Update(request.Content, request.IsInternal);
        await repository.UpdateCommentAsync(comment, cancellationToken);
        return Results.Ok(CommentResponse.FromEntity(comment));
    }

    private static async Task<IResult> DeleteComment(
        Guid id,
        Guid commentId,
        HttpContext context,
        [FromServices] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var comment = await repository.GetCommentByIdAsync(tenantId, commentId, cancellationToken);
        if (comment is null || comment.IncidentId != id)
        {
            return Results.NotFound();
        }

        await repository.DeleteCommentAsync(tenantId, commentId, cancellationToken);
        return Results.NoContent();
    }
}

public sealed record PatchIncidentRequest(
    IncidentStatus? Status,
    string? AssignedTo);

public sealed record CreateIncidentRequest(
    string Title,
    string Description,
    IncidentSeverity Severity,
    Guid? SourceAlertExecutionId);

public sealed record UpdateIncidentRequest(
    string Title,
    string Description,
    IncidentSeverity Severity,
    string? AssignedTo,
    IncidentStatus? Status);

public sealed record AddTimelineEntryRequest(
    IncidentTimelineEntryType EntryType,
    string Message,
    string? MetadataJson);

public sealed record AddCommentRequest(string Content, bool IsInternal, string Author = "system");

public sealed record UpdateCommentRequest(string Content, bool IsInternal);

public sealed record IncidentResponse(
    Guid Id,
    string Title,
    string Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    string? AssignedTo,
    Guid? SourceAlertExecutionId,
    string? CreatedBy,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static IncidentResponse FromEntity(Incident incident) => new(
        incident.Id,
        incident.Title,
        incident.Description,
        incident.Severity,
        incident.Status,
        incident.AssignedTo,
        incident.SourceAlertExecutionId,
        incident.CreatedBy,
        incident.ResolvedAt,
        incident.CreatedAt,
        incident.UpdatedAt);
}

public sealed record TimelineEntryResponse(
    Guid Id,
    IncidentTimelineEntryType EntryType,
    string Message,
    string? Actor,
    string? MetadataJson,
    DateTimeOffset CreatedAt)
{
    public static TimelineEntryResponse FromEntity(IncidentTimelineEntry entry) => new(
        entry.Id,
        entry.EntryType,
        entry.Message,
        entry.Actor,
        entry.MetadataJson,
        entry.CreatedAt);
}

public sealed record CommentResponse(
    Guid Id,
    string Author,
    string Content,
    bool IsInternal,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CommentResponse FromEntity(IncidentComment comment) => new(
        comment.Id,
        comment.Author,
        comment.Content,
        comment.IsInternal,
        comment.CreatedAt,
        comment.UpdatedAt);
}

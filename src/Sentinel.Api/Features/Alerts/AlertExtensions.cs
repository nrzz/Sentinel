using Microsoft.AspNetCore.Mvc;
using Sentinel.Api;
using Sentinel.Api.Authorization;
using Sentinel.Domain.Alerts;
using Sentinel.Infrastructure.Alerts;

namespace Sentinel.Api.Features.Alerts;

public static class AlertExtensions
{
    public static IServiceCollection AddAlertFeatures(this IServiceCollection services)
    {
        services.AddScoped<IAlertRepository, AlertRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alerts")
            .WithTags("Alerts")
            .RequireAuthorization();

        group.MapGet("/", ListAlertRules).RequireAuthorization(SentinelPolicies.AlertsRead);
        group.MapGet("/executions", ListRecentExecutions).RequireAuthorization(SentinelPolicies.AlertsRead);
        group.MapGet("/{id:guid}", GetAlertRule).RequireAuthorization(SentinelPolicies.AlertsRead);
        group.MapPost("/", CreateAlertRule).RequireAuthorization(SentinelPolicies.AlertsWrite);
        group.MapPut("/{id:guid}", UpdateAlertRule).RequireAuthorization(SentinelPolicies.AlertsWrite);
        group.MapDelete("/{id:guid}", DeleteAlertRule).RequireAuthorization(SentinelPolicies.AlertsDelete);
        group.MapGet("/{id:guid}/executions", ListAlertExecutions).RequireAuthorization(SentinelPolicies.AlertsRead);
        group.MapPost("/executions/{executionId:guid}/silence", SilenceAlertExecution)
            .RequireAuthorization(SentinelPolicies.AlertsWrite);

        return app;
    }

    private static async Task<IResult> ListAlertRules(
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rules = await repository.ListRulesAsync(tenantId, cancellationToken);
        return Results.Ok(rules.Select(AlertRuleResponse.FromEntity));
    }

    private static async Task<IResult> GetAlertRule(
        Guid id,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rule = await repository.GetRuleByIdAsync(tenantId, id, cancellationToken);
        return rule is null ? Results.NotFound() : Results.Ok(AlertRuleResponse.FromEntity(rule));
    }

    private static async Task<IResult> CreateAlertRule(
        CreateAlertRuleRequest request,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rule = AlertRule.Create(
            tenantId,
            request.Name,
            request.Description,
            request.Query,
            request.Condition,
            request.Severity,
            TimeSpan.FromSeconds(request.EvaluationIntervalSeconds),
            request.NotificationChannels,
            context.GetUserId());

        await repository.CreateRuleAsync(rule, cancellationToken);
        return Results.Created($"/api/v1/alerts/{rule.Id}", AlertRuleResponse.FromEntity(rule));
    }

    private static async Task<IResult> UpdateAlertRule(
        Guid id,
        UpdateAlertRuleRequest request,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rule = await repository.GetRuleByIdAsync(tenantId, id, cancellationToken);
        if (rule is null)
        {
            return Results.NotFound();
        }

        rule.Update(
            request.Name,
            request.Description,
            request.Query,
            request.Condition,
            request.Severity,
            TimeSpan.FromSeconds(request.EvaluationIntervalSeconds),
            request.NotificationChannels);

        if (request.Status.HasValue)
        {
            rule.SetStatus(request.Status.Value);
        }

        await repository.UpdateRuleAsync(rule, cancellationToken);
        return Results.Ok(AlertRuleResponse.FromEntity(rule));
    }

    private static async Task<IResult> DeleteAlertRule(
        Guid id,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rule = await repository.GetRuleByIdAsync(tenantId, id, cancellationToken);
        if (rule is null)
        {
            return Results.NotFound();
        }

        await repository.DeleteRuleAsync(tenantId, id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListAlertExecutions(
        Guid id,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var rule = await repository.GetRuleByIdAsync(tenantId, id, cancellationToken);
        if (rule is null)
        {
            return Results.NotFound();
        }

        var executions = await repository.ListExecutionsByRuleAsync(
            tenantId,
            id,
            limit > 0 ? limit : 100,
            cancellationToken);

        return Results.Ok(executions.Select(AlertExecutionResponse.FromEntity));
    }

    private static async Task<IResult> ListRecentExecutions(
        HttpContext context,
        [FromServices] IAlertRepository repository,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var executions = await repository.ListRecentExecutionsAsync(
            tenantId,
            limit > 0 ? limit : 100,
            cancellationToken);

        return Results.Ok(executions.Select(AlertExecutionResponse.FromEntity));
    }

    private static async Task<IResult> SilenceAlertExecution(
        Guid executionId,
        SilenceAlertExecutionRequest request,
        HttpContext context,
        [FromServices] IAlertRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var execution = await repository.GetExecutionByIdAsync(tenantId, executionId, cancellationToken);
        if (execution is null)
        {
            return Results.NotFound();
        }

        if (execution.Status != AlertExecutionStatus.Triggered)
        {
            return Results.Conflict(new { error = "Only triggered executions can be silenced." });
        }

        var duration = request.DurationMinutes > 0 ? request.DurationMinutes : 60;
        execution.Suppress($"silenced for {duration} minutes");
        await repository.UpdateExecutionAsync(execution, cancellationToken);

        return Results.Ok(AlertExecutionResponse.FromEntity(execution));
    }
}

public sealed record SilenceAlertExecutionRequest(int DurationMinutes);

public sealed record CreateAlertRuleRequest(
    string Name,
    string Description,
    string Query,
    string Condition,
    AlertSeverity Severity,
    long EvaluationIntervalSeconds,
    IReadOnlyList<string> NotificationChannels);

public sealed record UpdateAlertRuleRequest(
    string Name,
    string Description,
    string Query,
    string Condition,
    AlertSeverity Severity,
    long EvaluationIntervalSeconds,
    IReadOnlyList<string> NotificationChannels,
    AlertRuleStatus? Status);

public sealed record AlertRuleResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Description,
    string Query,
    string Condition,
    AlertSeverity Severity,
    AlertRuleStatus Status,
    long EvaluationIntervalSeconds,
    IReadOnlyList<string> NotificationChannels,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static AlertRuleResponse FromEntity(AlertRule rule) => new(
        rule.Id,
        rule.TenantId,
        rule.Name,
        rule.Description,
        rule.Query,
        rule.Condition,
        rule.Severity,
        rule.Status,
        (long)rule.EvaluationInterval.TotalSeconds,
        rule.NotificationChannels,
        rule.CreatedBy,
        rule.CreatedAt,
        rule.UpdatedAt);
}

public sealed record AlertExecutionResponse(
    Guid Id,
    Guid AlertRuleId,
    AlertExecutionStatus Status,
    AlertSeverity Severity,
    string Message,
    string? MatchedValue,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ResolvedAt,
    string? CorrelationId,
    DateTimeOffset CreatedAt)
{
    public static AlertExecutionResponse FromEntity(AlertExecution execution) => new(
        execution.Id,
        execution.AlertRuleId,
        execution.Status,
        execution.Severity,
        execution.Message,
        execution.MatchedValue,
        execution.TriggeredAt,
        execution.ResolvedAt,
        execution.CorrelationId,
        execution.CreatedAt);
}

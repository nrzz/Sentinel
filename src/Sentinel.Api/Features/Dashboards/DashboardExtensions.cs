using Microsoft.AspNetCore.Mvc;
using Sentinel.Api;
using Sentinel.Api.Authorization;
using Sentinel.Domain.Dashboards;
using Sentinel.Infrastructure.Dashboards;

namespace Sentinel.Api.Features.Dashboards;

public static class DashboardExtensions
{
    public static IServiceCollection AddDashboardFeatures(this IServiceCollection services)
    {
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboards")
            .WithTags("Dashboards")
            .RequireAuthorization();

        group.MapGet("/", ListDashboards).RequireAuthorization(SentinelPolicies.DashboardsRead);
        group.MapGet("/{id:guid}", GetDashboard).RequireAuthorization(SentinelPolicies.DashboardsRead);
        group.MapPost("/", CreateDashboard).RequireAuthorization(SentinelPolicies.DashboardsWrite);
        group.MapPut("/{id:guid}", UpdateDashboard).RequireAuthorization(SentinelPolicies.DashboardsWrite);
        group.MapDelete("/{id:guid}", DeleteDashboard).RequireAuthorization(SentinelPolicies.DashboardsDelete);

        return app;
    }

    private static async Task<IResult> ListDashboards(
        HttpContext context,
        [FromServices] IDashboardRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var dashboards = await repository.ListAsync(tenantId, cancellationToken);
        return Results.Ok(dashboards.Select(DashboardResponse.FromEntity));
    }

    private static async Task<IResult> GetDashboard(
        Guid id,
        HttpContext context,
        [FromServices] IDashboardRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var dashboard = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        return dashboard is null ? Results.NotFound() : Results.Ok(DashboardResponse.FromEntity(dashboard));
    }

    private static async Task<IResult> CreateDashboard(
        CreateDashboardRequest request,
        HttpContext context,
        [FromServices] IDashboardRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var dashboard = Dashboard.Create(
            tenantId,
            request.Name,
            request.Description,
            request.LayoutJson,
            request.IsDefault,
            context.GetUserId());

        await repository.CreateAsync(dashboard, cancellationToken);
        return Results.Created($"/api/v1/dashboards/{dashboard.Id}", DashboardResponse.FromEntity(dashboard));
    }

    private static async Task<IResult> UpdateDashboard(
        Guid id,
        UpdateDashboardRequest request,
        HttpContext context,
        [FromServices] IDashboardRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var dashboard = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (dashboard is null)
        {
            return Results.NotFound();
        }

        dashboard.Update(request.Name, request.Description, request.LayoutJson, request.IsDefault);
        await repository.UpdateAsync(dashboard, cancellationToken);
        return Results.Ok(DashboardResponse.FromEntity(dashboard));
    }

    private static async Task<IResult> DeleteDashboard(
        Guid id,
        HttpContext context,
        [FromServices] IDashboardRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var dashboard = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (dashboard is null)
        {
            return Results.NotFound();
        }

        await repository.DeleteAsync(tenantId, id, cancellationToken);
        return Results.NoContent();
    }
}

public sealed record CreateDashboardRequest(
    string Name,
    string Description,
    string LayoutJson,
    bool IsDefault);

public sealed record UpdateDashboardRequest(
    string Name,
    string Description,
    string LayoutJson,
    bool IsDefault);

public sealed record DashboardResponse(
    Guid Id,
    string Name,
    string Description,
    string LayoutJson,
    bool IsDefault,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static DashboardResponse FromEntity(Dashboard dashboard) => new(
        dashboard.Id,
        dashboard.Name,
        dashboard.Description,
        dashboard.LayoutJson,
        dashboard.IsDefault,
        dashboard.CreatedBy,
        dashboard.CreatedAt,
        dashboard.UpdatedAt);
}

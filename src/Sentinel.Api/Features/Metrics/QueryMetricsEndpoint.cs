using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Common;
using Sentinel.Infrastructure.Persistence.ClickHouse;
using System.Text.Json;

namespace Sentinel.Api.Features.Metrics;

public static class QueryMetricsEndpoint
{
    public static void MapQueryMetricsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/metrics", HandleAsync)
            .WithName("QueryMetrics")
            .WithTags("Metrics")
            .Produces<QueryMetricsResponse>()
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] QueryMetricsRequest query,
        HttpContext httpContext,
        IMetricRepository metricRepository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var filters = new MetricQueryFilters(
            TenantId: tenantId,
            From: query.From,
            To: query.To,
            Name: query.Name,
            Service: query.Service,
            Environment: query.Environment,
            Limit: Math.Clamp(query.Limit, 1, 10_000));

        var metrics = await metricRepository.QueryAsync(filters, cancellationToken);
        var items = metrics.Select(metric =>
        {
            var tags = string.IsNullOrWhiteSpace(metric.TagsJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(metric.TagsJson)
                  ?? new Dictionary<string, string>();

            return new MetricItemDto(
                metric.Id,
                metric.Timestamp,
                metric.Name,
                metric.Value,
                metric.Unit,
                metric.Service,
                metric.Environment,
                tags);
        }).ToList();

        return Results.Ok(new QueryMetricsResponse(items));
    }
}

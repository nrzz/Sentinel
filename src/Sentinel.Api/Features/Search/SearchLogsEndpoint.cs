using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Common;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Api.Features.Search;

public static class SearchLogsEndpoint
{
    public static void MapSearchLogsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/search/logs", HandleAsync)
            .WithName("SearchLogs")
            .WithTags("Search")
            .Produces<SearchLogsResponse>()
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] SearchLogsQuery query,
        HttpContext httpContext,
        ILogRepository logRepository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var limit = Math.Clamp(query.Limit, 1, 1000);
        var offset = Math.Max(query.Offset, 0);

        var filters = new LogSearchFilters(
            TenantId: tenantId,
            From: query.From,
            To: query.To,
            Level: query.Level,
            Service: query.Service,
            Environment: query.Environment,
            Query: query.Query,
            TraceId: query.TraceId,
            Limit: limit,
            Offset: offset);

        var result = await logRepository.SearchAsync(filters, cancellationToken);
        var items = result.Items.Select(log => new LogSearchItemDto(
            log.Id,
            log.Timestamp,
            log.Service,
            log.Environment,
            log.Level,
            log.NormalizedLevel,
            log.Message,
            log.TraceId,
            log.SpanId,
            log.CorrelationId,
            log.ParsedException,
            log.SourceHost)).ToList();

        return Results.Ok(new SearchLogsResponse(items, result.TotalCount, limit, offset));
    }
}

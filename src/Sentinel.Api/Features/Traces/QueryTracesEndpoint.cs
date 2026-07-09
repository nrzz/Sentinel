using Microsoft.AspNetCore.Mvc;
using Sentinel.Api.Authorization;
using Sentinel.Api.Common;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.Api.Features.Traces;

public static class QueryTracesEndpoint
{
    public static void MapQueryTracesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/traces", ListAsync)
            .WithName("QueryTraces")
            .WithTags("Traces")
            .Produces<QueryTracesResponse>()
            .RequireAuthorization(SentinelPolicies.TracesRead);

        app.MapGet("/api/v1/traces/{traceId}", GetDetailAsync)
            .WithName("GetTraceDetail")
            .WithTags("Traces")
            .Produces<GetTraceDetailResponse>()
            .RequireAuthorization(SentinelPolicies.TracesRead);
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] QueryTracesRequest query,
        HttpContext httpContext,
        ITraceRepository traceRepository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var filters = new TraceQueryFilters(
            TenantId: tenantId,
            From: query.From,
            To: query.To,
            Service: query.Service,
            TraceId: query.TraceId,
            Status: query.Status,
            Limit: Math.Clamp(query.Limit, 1, 1000));

        var traces = await traceRepository.QueryTracesAsync(filters, cancellationToken);
        var items = traces.Select(trace => new TraceItemDto(
            trace.Id,
            trace.TraceId,
            trace.Timestamp,
            trace.Service,
            trace.Name,
            trace.DurationMs,
            trace.Status)).ToList();

        return Results.Ok(new QueryTracesResponse(items));
    }

    private static async Task<IResult> GetDetailAsync(
        string traceId,
        HttpContext httpContext,
        ITraceRepository traceRepository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var filters = new TraceQueryFilters(
            TenantId: tenantId,
            From: null,
            To: null,
            Service: null,
            TraceId: traceId,
            Status: null,
            Limit: 1);
        var traces = await traceRepository.QueryTracesAsync(filters, cancellationToken);
        var trace = traces.FirstOrDefault();
        if (trace is null)
        {
            return Results.NotFound();
        }

        var spans = await traceRepository.GetSpansByTraceIdAsync(tenantId, traceId, cancellationToken);
        var response = new GetTraceDetailResponse(
            new TraceItemDto(trace.Id, trace.TraceId, trace.Timestamp, trace.Service, trace.Name, trace.DurationMs, trace.Status),
            spans.Select(span => new TraceSpanItemDto(
                span.Id,
                span.SpanId,
                span.ParentSpanId,
                span.Timestamp,
                span.Name,
                span.Service,
                span.DurationMs,
                span.Status)).ToList());

        return Results.Ok(response);
    }
}

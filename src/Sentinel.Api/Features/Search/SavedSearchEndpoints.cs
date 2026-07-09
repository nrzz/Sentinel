using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sentinel.Api.Authorization;
using Sentinel.Api.Common;
using Sentinel.Infrastructure.Persistence.Search;

namespace Sentinel.Api.Features.Search;

public static class SavedSearchEndpoints
{
    public static void MapSavedSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/search/saved").WithTags("Search").RequireAuthorization();

        group.MapGet("/", ListAsync).WithName("ListSavedSearches").RequireAuthorization(SentinelPolicies.SearchRead);
        group.MapGet("/{id:guid}", GetAsync).WithName("GetSavedSearch").RequireAuthorization(SentinelPolicies.SearchRead);
        group.MapPost("/", CreateAsync).WithName("CreateSavedSearch").RequireAuthorization(SentinelPolicies.SearchWrite);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateSavedSearch").RequireAuthorization(SentinelPolicies.SearchWrite);
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteSavedSearch").RequireAuthorization(SentinelPolicies.SearchWrite);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        ISavedSearchRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var searches = await repository.ListAsync(tenantId, cancellationToken);
        return Results.Ok(searches.Select(ToDto));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext httpContext,
        ISavedSearchRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var search = await repository.GetAsync(tenantId, id, cancellationToken);
        return search is null ? Results.NotFound() : Results.Ok(ToDto(search));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateSavedSearchRequest request,
        HttpContext httpContext,
        IValidator<CreateSavedSearchRequest> validator,
        ISavedSearchRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var now = DateTimeOffset.UtcNow;
        var search = new SavedSearch(
            Id: Guid.NewGuid(),
            TenantId: tenantId,
            Name: request.Name,
            Query: request.Query,
            Level: request.Level,
            Service: request.Service,
            Environment: request.Environment,
            CreatedAt: now,
            UpdatedAt: now);

        var created = await repository.CreateAsync(search, cancellationToken);
        return Results.Created($"/api/v1/search/saved/{created.Id}", ToDto(created));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateSavedSearchRequest request,
        HttpContext httpContext,
        IValidator<UpdateSavedSearchRequest> validator,
        ISavedSearchRepository repository,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var existing = await repository.GetAsync(tenantId, id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        var updated = existing with
        {
            Name = request.Name,
            Query = request.Query,
            Level = request.Level,
            Service = request.Service,
            Environment = request.Environment,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await repository.UpdateAsync(updated, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(ToDto(result));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        HttpContext httpContext,
        ISavedSearchRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantResolver.ResolveTenantId(httpContext);
        var deleted = await repository.DeleteAsync(tenantId, id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static SavedSearchDto ToDto(SavedSearch search) =>
        new(search.Id, search.Name, search.Query, search.Level, search.Service, search.Environment, search.CreatedAt, search.UpdatedAt);
}

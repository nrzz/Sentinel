using Sentinel.Api.Authorization;

namespace Sentinel.Api.Features.Tenants;

public static class TenantExtensions
{
    public static IServiceCollection AddTenantFeatures(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        group.MapGet("/", ListTenantsEndpoint.Handle)
            .WithName("ListTenants")
            .RequireAuthorization(SentinelPolicies.TenantsRead)
            .Produces<IReadOnlyList<TenantResponse>>();

        group.MapGet("/{id:guid}", GetTenantEndpoint.Handle)
            .WithName("GetTenant")
            .RequireAuthorization(SentinelPolicies.TenantsRead)
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTenantEndpoint.Handle)
            .WithName("CreateTenant")
            .RequireAuthorization(SentinelPolicies.TenantsWrite)
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateTenantEndpoint.Handle)
            .WithName("UpdateTenant")
            .RequireAuthorization(SentinelPolicies.TenantsWrite)
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteTenantEndpoint.Handle)
            .WithName("DeleteTenant")
            .RequireAuthorization(SentinelPolicies.TenantsDelete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/members", ListTenantMembersEndpoint.Handle)
            .WithName("ListTenantMembers")
            .RequireAuthorization(SentinelPolicies.TenantsRead)
            .Produces<IReadOnlyList<TenantMemberResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/members", AddTenantMemberEndpoint.Handle)
            .WithName("AddTenantMember")
            .RequireAuthorization(SentinelPolicies.TenantsWrite)
            .Produces<TenantMemberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/members/{userId:guid}", RemoveTenantMemberEndpoint.Handle)
            .WithName("RemoveTenantMember")
            .RequireAuthorization(SentinelPolicies.TenantsDelete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

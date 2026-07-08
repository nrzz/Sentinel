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
            .Produces<IReadOnlyList<TenantResponse>>();

        group.MapGet("/{id:guid}", GetTenantEndpoint.Handle)
            .WithName("GetTenant")
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTenantEndpoint.Handle)
            .WithName("CreateTenant")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateTenantEndpoint.Handle)
            .WithName("UpdateTenant")
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteTenantEndpoint.Handle)
            .WithName("DeleteTenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/members", ListTenantMembersEndpoint.Handle)
            .WithName("ListTenantMembers")
            .Produces<IReadOnlyList<TenantMemberResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/members", AddTenantMemberEndpoint.Handle)
            .WithName("AddTenantMember")
            .Produces<TenantMemberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/members/{userId:guid}", RemoveTenantMemberEndpoint.Handle)
            .WithName("RemoveTenantMember")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

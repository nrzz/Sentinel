using Microsoft.AspNetCore.Mvc;
using Sentinel.Api;
using Sentinel.Domain.Plugins;
using Sentinel.Infrastructure.Plugins;

namespace Sentinel.Api.Features.Plugins;

public static class PluginExtensions
{
    public static IServiceCollection AddPluginFeatures(this IServiceCollection services)
    {
        services.AddScoped<IPluginRepository, PluginRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/plugins")
            .WithTags("Plugins")
            .RequireAuthorization();

        group.MapGet("/", ListPlugins);
        group.MapPost("/install", InstallPlugin);
        group.MapDelete("/{id:guid}", RemovePlugin);

        return app;
    }

    private static async Task<IResult> ListPlugins(
        HttpContext context,
        [FromServices] IPluginRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var plugins = await repository.ListAsync(tenantId, cancellationToken);
        return Results.Ok(plugins.Select(PluginResponse.FromEntity));
    }

    private static async Task<IResult> InstallPlugin(
        InstallPluginRequest request,
        HttpContext context,
        [FromServices] IPluginRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var existing = await repository.GetByNameAsync(tenantId, request.Name, cancellationToken);
        if (existing is not null)
        {
            return Results.Conflict(new { error = $"Plugin '{request.Name}' is already installed." });
        }

        var plugin = Plugin.Create(
            tenantId,
            request.Name,
            request.Version,
            request.Description,
            request.AssemblyName,
            request.ConfigurationJson,
            context.GetUserId());

        plugin.Enable();
        await repository.InstallAsync(plugin, cancellationToken);

        return Results.Created($"/api/v1/plugins/{plugin.Id}", PluginResponse.FromEntity(plugin));
    }

    private static async Task<IResult> RemovePlugin(
        Guid id,
        HttpContext context,
        [FromServices] IPluginRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var plugin = await repository.GetByIdAsync(tenantId, id, cancellationToken);
        if (plugin is null)
        {
            return Results.NotFound();
        }

        await repository.RemoveAsync(tenantId, id, cancellationToken);
        return Results.NoContent();
    }
}

public sealed record InstallPluginRequest(
    string Name,
    string Version,
    string Description,
    string AssemblyName,
    string ConfigurationJson);

public sealed record PluginResponse(
    Guid Id,
    string Name,
    string Version,
    string Description,
    string AssemblyName,
    string ConfigurationJson,
    PluginStatus Status,
    string? InstalledBy,
    DateTimeOffset InstalledAt,
    DateTimeOffset CreatedAt)
{
    public static PluginResponse FromEntity(Plugin plugin) => new(
        plugin.Id,
        plugin.Name,
        plugin.Version,
        plugin.Description,
        plugin.AssemblyName,
        plugin.ConfigurationJson,
        plugin.Status,
        plugin.InstalledBy,
        plugin.InstalledAt,
        plugin.CreatedAt);
}

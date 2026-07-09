using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Sentinel.Api.Authentication;
using Sentinel.Domain.Configuration;

namespace Sentinel.Api.Features.Ingestion;

public static class IngestionAuthExtensions
{
    public static RouteHandlerBuilder WithIngestionAuth(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<IngestionAuthFilter>();
    }
}

internal sealed class IngestionAuthFilter : IEndpointFilter
{
    private readonly IOptionsMonitor<IngestionOptions> _options;

    public IngestionAuthFilter(IOptionsMonitor<IngestionOptions> options)
    {
        _options = options;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var options = _options.CurrentValue;
        var httpContext = context.HttpContext;

        if (options.RequireApiKey)
        {
            var apiKeyResult = await httpContext.AuthenticateAsync(ApiKeyAuthenticationDefaults.AuthenticationScheme);
            if (apiKeyResult.Succeeded && apiKeyResult.Principal is not null)
            {
                httpContext.User = apiKeyResult.Principal;
                return await next(context);
            }

            var jwtResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (jwtResult.Succeeded && jwtResult.Principal is not null)
            {
                httpContext.User = jwtResult.Principal;
                return await next(context);
            }

            return Results.Problem(
                "A valid API key or bearer token is required for ingestion.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!options.AllowHeaderOnlyTenant)
        {
            return Results.Problem(
                "Ingestion is not configured for anonymous access.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}

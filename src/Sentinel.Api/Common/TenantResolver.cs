using Sentinel.Domain.Common;

namespace Sentinel.Api.Common;

public static class TenantResolver
{
    public static Guid ResolveTenantId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationId.TenantHeaderName, out var headerValue)
            && Guid.TryParse(headerValue, out var tenantId))
        {
            return tenantId;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("tenant_id")?.Value;
            if (claim is not null && Guid.TryParse(claim, out tenantId))
            {
                return tenantId;
            }
        }

        throw new UnauthorizedAccessException("A valid tenant identifier is required.");
    }
}

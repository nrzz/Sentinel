using System.Security.Claims;
using Sentinel.Domain.Common;

namespace Sentinel.Api;

internal static class TenantHttpExtensions
{
    internal static Guid GetTenantId(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationId.TenantHeaderName, out var tenantHeader)
            && Guid.TryParse(tenantHeader.FirstOrDefault(), out var headerTenantId))
        {
            return headerTenantId;
        }

        var claim = context.User.FindFirstValue("tenant_id");
        if (claim is not null && Guid.TryParse(claim, out var claimTenantId))
        {
            return claimTenantId;
        }

        return Guid.Empty;
    }

    internal static string? GetUserId(this HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue(ClaimTypes.Name);

    internal static string? GetCorrelationId(this HttpContext context) =>
        context.Items[CorrelationId.HeaderName]?.ToString();
}

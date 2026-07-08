using Microsoft.AspNetCore.Http;
using Sentinel.Domain.Common;

namespace Sentinel.Infrastructure.Identity;

public sealed class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _overrideTenantId;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            if (_overrideTenantId.HasValue)
            {
                return _overrideTenantId;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Request.Headers.TryGetValue(CorrelationId.TenantHeaderName, out var headerValue)
                && Guid.TryParse(headerValue.ToString(), out var tenantId))
            {
                return tenantId;
            }

            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out tenantId))
            {
                return tenantId;
            }

            return null;
        }
    }

    public bool HasTenant => TenantId.HasValue;

    public Guid RequireTenantId()
    {
        return TenantId ?? throw new InvalidOperationException("Tenant context is required but was not provided.");
    }

    public void SetTenantId(Guid tenantId) => _overrideTenantId = tenantId;
}

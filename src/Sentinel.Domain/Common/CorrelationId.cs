namespace Sentinel.Domain.Common;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string TenantHeaderName = "X-Tenant-ID";

    public static string New() => Guid.NewGuid().ToString("N");
}

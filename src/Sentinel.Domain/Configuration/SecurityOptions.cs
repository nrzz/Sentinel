namespace Sentinel.Domain.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool AllowOpenRegistration { get; set; }
    public bool SeedDefaultAdmin { get; set; } = true;
    public string[] AllowedCorsOrigins { get; set; } = [];
}

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>
    /// When true, ingestion endpoints require a valid tenant API key or JWT with write permissions.
    /// </summary>
    public bool RequireApiKey { get; set; }

    /// <summary>
    /// Dev-only: allow anonymous ingestion with only an X-Tenant-ID header (no API key).
    /// </summary>
    public bool AllowHeaderOnlyTenant { get; set; } = true;
}

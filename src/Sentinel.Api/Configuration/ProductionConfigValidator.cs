using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;

namespace Sentinel.Api.Configuration;

public static class ProductionConfigValidator
{
    private const string DefaultJwtSecret = "sentinel-dev-secret-key-change-in-production-min-32-chars";
    private const string DefaultPostgresPassword = "sentinel_dev_password";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var errors = new List<string>();

        var jwtSecret = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()?.SecretKey;
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == DefaultJwtSecret)
        {
            errors.Add("Jwt:SecretKey must be set to a non-default value in Production.");
        }

        var postgresConnection = configuration.GetSection(PostgreSqlOptions.SectionName)
            .Get<PostgreSqlOptions>()?.ConnectionString ?? string.Empty;
        if (postgresConnection.Contains(DefaultPostgresPassword, StringComparison.Ordinal))
        {
            errors.Add("PostgreSQL connection string must not use the default development password in Production.");
        }

        var security = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
        if (security.SeedDefaultAdmin)
        {
            errors.Add("Security:SeedDefaultAdmin must be false in Production.");
        }

        var ingestion = configuration.GetSection(IngestionOptions.SectionName).Get<IngestionOptions>() ?? new IngestionOptions();
        if (ingestion.AllowHeaderOnlyTenant)
        {
            errors.Add("Ingestion:AllowHeaderOnlyTenant must be false in Production.");
        }

        if (!ingestion.RequireApiKey)
        {
            errors.Add("Ingestion:RequireApiKey must be true in Production.");
        }

        var corsOrigins = security.AllowedCorsOrigins;
        if (corsOrigins.Length == 0)
        {
            errors.Add("Security:AllowedCorsOrigins must specify at least one origin in Production.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration validation failed:\n- " + string.Join("\n- ", errors));
        }
    }
}

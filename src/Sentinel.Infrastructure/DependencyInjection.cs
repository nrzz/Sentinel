using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.Health;
using Sentinel.Infrastructure.Identity;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;
using Sentinel.Infrastructure.Persistence.PostgreSQL;
using Sentinel.Infrastructure.Persistence.Redis;
using Sentinel.Infrastructure.Persistence.Search;

namespace Sentinel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSentinelInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SentinelOptions>(configuration.GetSection(SentinelOptions.SectionName));
        services.Configure<PostgreSqlOptions>(configuration.GetSection(PostgreSqlOptions.SectionName));
        services.Configure<ClickHouseOptions>(configuration.GetSection(ClickHouseOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<IngestionOptions>(configuration.GetSection(IngestionOptions.SectionName));

        services.AddSingleton<IPostgreSqlConnectionFactory, PostgreSqlConnectionFactory>();
        services.AddSingleton<IClickHouseConnectionFactory, ClickHouseConnectionFactory>();
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddSingleton<IRabbitMqTopologyInitializer, RabbitMqTopologyInitializer>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRbacService, RbacService>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITenantService, TenantService>();

        var postgresConnectionString = configuration.GetSection(PostgreSqlOptions.SectionName)
            .Get<PostgreSqlOptions>()?.ConnectionString
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured.");

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(postgresConnectionString)
                .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        services.AddSingleton<Persistence.PostgreSQL.IMigrationRunner, Persistence.PostgreSQL.MigrationRunner>();
        services.AddSingleton<IDataSeeder, DataSeeder>();

        services.AddSingleton<IClickHouseMigrator, ClickHouseMigrator>();
        services.AddSingleton<ILogRepository, LogRepository>();
        services.AddSingleton<IMetricRepository, MetricRepository>();
        services.AddSingleton<ITraceRepository, TraceRepository>();
        services.AddSingleton<ISavedSearchRepository, SavedSearchRepository>();

        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready", "db"])
            .AddCheck<ClickHouseHealthCheck>("clickhouse", tags: ["ready", "db"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "cache"])
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready", "messaging"]);

        return services;
    }
}

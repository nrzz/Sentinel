using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sentinel.Infrastructure.Alerts;
using Sentinel.Infrastructure.Identity;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.IntegrationTests.Infrastructure;

public class SentinelWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemoryLogRepository LogRepository { get; } = new();
    public InMemoryAlertRepository AlertRepository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PostgreSQL:ConnectionString"] = "Host=localhost;Port=5432;Database=sentinel_test;Username=sentinel;Password=test",
                ["ClickHouse:ConnectionString"] = "Host=localhost;Port=8123;Database=sentinel;Username=default;Password=",
                ["Redis:ConnectionString"] = "localhost:6379",
                ["RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672",
                ["Jwt:SecretKey"] = "integration-test-secret-key-min-32-chars-long",
                ["Jwt:Issuer"] = "sentinel",
                ["Jwt:Audience"] = "sentinel-api",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILogRepository>();
            services.RemoveAll<IRabbitMqPublisher>();
            services.RemoveAll<IAuthService>();
            services.RemoveAll<IAlertRepository>();
            services.RemoveAll<IRabbitMqTopologyInitializer>();
            services.RemoveAll<IMigrationRunner>();
            services.RemoveAll<IDataSeeder>();
            services.RemoveAll<IClickHouseMigrator>();

            services.AddSingleton(LogRepository);
            services.AddSingleton<ILogRepository>(LogRepository);
            services.AddSingleton<IRabbitMqPublisher>(new InMemoryRabbitMqPublisher(LogRepository));
            services.AddSingleton<IAuthService, TestAuthService>();
            services.AddSingleton(AlertRepository);
            services.AddSingleton<IAlertRepository>(AlertRepository);
            services.AddSingleton<IRabbitMqTopologyInitializer, NoOpRabbitMqTopologyInitializer>();
            services.AddSingleton<IMigrationRunner, NoOpMigrationRunner>();
            services.AddSingleton<IDataSeeder, NoOpDataSeeder>();
            services.AddSingleton<IClickHouseMigrator, NoOpClickHouseMigrator>();

            services.PostConfigure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
        });
    }

    public HttpClient CreateAuthenticatedClient(string? accessToken = null, Guid? tenantId = null)
    {
        var client = CreateClient();
        if (accessToken is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (tenantId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.Value.ToString());
        }

        return client;
    }

    public void ResetState()
    {
        LogRepository.Clear();
        AlertRepository.Clear();
        TestAuthService.Reset();
    }
}

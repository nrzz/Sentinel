using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentMigratorRunner = FluentMigrator.Runner.IMigrationRunner;

namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

public sealed class MigrationRunner : IMigrationRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(IServiceProvider serviceProvider, ILogger<MigrationRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running PostgreSQL migrations");

        using var scope = _serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<FluentMigratorRunner>();
        runner.MigrateUp();

        _logger.LogInformation("PostgreSQL migrations completed");
        return Task.CompletedTask;
    }
}

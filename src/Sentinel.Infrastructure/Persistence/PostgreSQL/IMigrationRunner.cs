namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

public interface IMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

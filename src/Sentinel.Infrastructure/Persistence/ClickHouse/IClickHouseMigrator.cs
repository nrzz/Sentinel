namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public interface IClickHouseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

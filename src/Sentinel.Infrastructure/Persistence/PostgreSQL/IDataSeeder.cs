namespace Sentinel.Infrastructure.Persistence.PostgreSQL;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

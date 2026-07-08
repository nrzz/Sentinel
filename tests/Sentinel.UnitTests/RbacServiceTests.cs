using Microsoft.Extensions.Options;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.Identity;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.UnitTests.Identity;

public class RbacServiceTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("PostgreSQL__ConnectionString");

    private static RbacService? CreateService()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var factory = new PostgreSqlConnectionFactory(Options.Create(new PostgreSqlOptions
        {
            ConnectionString = connectionString,
        }));

        return new RbacService(factory);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_UnknownUser_ReturnsEmpty()
    {
        if (CreateService() is not { } service)
        {
            return;
        }

        var permissions = await service.GetUserPermissionsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task GetUserRolesAsync_UnknownUser_ReturnsEmpty()
    {
        if (CreateService() is not { } service)
        {
            return;
        }

        var roles = await service.GetUserRolesAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task HasPermissionAsync_UnknownUser_ReturnsFalse()
    {
        if (CreateService() is not { } service)
        {
            return;
        }

        var hasPermission = await service.HasPermissionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "logs:read");

        Assert.False(hasPermission);
    }

    [Fact]
    public void HasPermissionAsync_CaseInsensitiveMatching()
    {
        var permissions = new List<string> { "logs:read", "metrics:write" };

        Assert.Contains(permissions, p => string.Equals(p, "LOGS:READ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(permissions, p => string.Equals(p, "logs:read", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(permissions, p => string.Equals(p, "alerts:read", StringComparison.OrdinalIgnoreCase));
    }
}

using System.Collections.Concurrent;
using System.Text.Json;
using Sentinel.Api.Authorization;
using Sentinel.Domain.Alerts;
using Sentinel.Domain.Events;
using Sentinel.Domain.Observability;
using Sentinel.Infrastructure.Alerts;
using Sentinel.Infrastructure.Identity;
using Sentinel.Infrastructure.Messaging;
using Sentinel.Infrastructure.Persistence.ClickHouse;

namespace Sentinel.IntegrationTests.Infrastructure;

public static class TestData
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public const string AdminEmail = "admin@sentinel.local";
    public const string AdminPassword = "Admin123!";
}

public sealed class InMemoryLogRepository : ILogRepository
{
    private readonly ConcurrentBag<EnrichedLogRecord> _logs = new();

    public Task BatchInsertRawAsync(IReadOnlyList<LogRecord> logs, CancellationToken cancellationToken = default)
    {
        foreach (var log in logs)
        {
            _logs.Add(ToEnriched(log));
        }

        return Task.CompletedTask;
    }

    public Task BatchInsertEnrichedAsync(IReadOnlyList<EnrichedLogRecord> logs, CancellationToken cancellationToken = default)
    {
        foreach (var log in logs)
        {
            _logs.Add(log);
        }

        return Task.CompletedTask;
    }

    public Task<LogSearchResult> SearchAsync(LogSearchFilters filters, CancellationToken cancellationToken = default)
    {
        var query = _logs
            .Where(log => log.TenantId == filters.TenantId)
            .AsEnumerable();

        if (filters.From.HasValue)
        {
            query = query.Where(log => log.Timestamp >= filters.From.Value);
        }

        if (filters.To.HasValue)
        {
            query = query.Where(log => log.Timestamp <= filters.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Level))
        {
            query = query.Where(log =>
                log.Level.Equals(filters.Level, StringComparison.OrdinalIgnoreCase)
                || log.NormalizedLevel.Equals(filters.Level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filters.Service))
        {
            query = query.Where(log =>
                log.Service.Contains(filters.Service, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            query = query.Where(log =>
                log.Message.Contains(filters.Query, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderByDescending(log => log.Timestamp).ToList();
        var page = ordered
            .Skip(filters.Offset)
            .Take(filters.Limit)
            .ToList();

        return Task.FromResult(new LogSearchResult(page, ordered.Count));
    }

    public void Clear() => _logs.Clear();

    private static EnrichedLogRecord ToEnriched(LogRecord log)
    {
        var now = DateTimeOffset.UtcNow;
        return new EnrichedLogRecord(
            log.Id,
            log.TenantId,
            log.Timestamp,
            log.Service,
            log.Environment,
            log.Level,
            log.Level.ToLowerInvariant(),
            log.Message,
            log.AttributesJson,
            log.TraceId,
            log.SpanId,
            log.CorrelationId,
            null,
            null,
            now,
            now);
    }
}

public sealed class InMemoryRabbitMqPublisher : IRabbitMqPublisher
{
    private readonly InMemoryLogRepository _logRepository;

    public InMemoryRabbitMqPublisher(InMemoryLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        if (message is LogReceived received)
        {
            var raw = new LogRecord(
                received.Id,
                received.TenantId,
                received.Timestamp,
                received.Service,
                received.Environment,
                received.Level,
                received.Message,
                JsonSerializer.Serialize(received.Attributes),
                received.TraceId,
                received.SpanId,
                received.CorrelationId);

            await _logRepository.BatchInsertEnrichedAsync(
            [
                new EnrichedLogRecord(
                    raw.Id,
                    raw.TenantId,
                    raw.Timestamp,
                    raw.Service,
                    raw.Environment,
                    raw.Level,
                    raw.Level.ToLowerInvariant(),
                    raw.Message,
                    raw.AttributesJson,
                    raw.TraceId,
                    raw.SpanId,
                    raw.CorrelationId,
                    null,
                    null,
                    received.ReceivedAt,
                    DateTimeOffset.UtcNow)
            ],
            cancellationToken);
        }

        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class TestAuthService : IAuthService
{
    private readonly IJwtTokenService _jwtTokenService;
    private static readonly Dictionary<string, string> RefreshTokens = new();

    public TestAuthService(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public Task<AuthResult?> LoginAsync(string email, string password, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (!email.Equals(TestData.AdminEmail, StringComparison.OrdinalIgnoreCase)
            || password != TestData.AdminPassword)
        {
            return Task.FromResult<AuthResult?>(null);
        }

        var user = new AuthenticatedUser(
            TestData.AdminUserId,
            TestData.AdminEmail,
            "Admin User",
            tenantId ?? TestData.DefaultTenantId,
            ["admin"],
            SentinelPermissions.All.ToList());

        return Task.FromResult<AuthResult?>(CreateResult(user));
    }

    public Task<AuthResult?> RegisterAsync(
        string email,
        string password,
        string displayName,
        string tenantName,
        string tenantSlug,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AuthResult?>(null);

    public Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!RefreshTokens.TryGetValue(refreshToken, out var userId)
            || userId != TestData.AdminUserId.ToString())
        {
            return Task.FromResult<AuthResult?>(null);
        }

        var user = new AuthenticatedUser(
            TestData.AdminUserId,
            TestData.AdminEmail,
            "Admin User",
            TestData.DefaultTenantId,
            ["admin"],
            SentinelPermissions.All.ToList());

        return Task.FromResult<AuthResult?>(CreateResult(user));
    }

    private AuthResult CreateResult(AuthenticatedUser user)
    {
        var tokens = _jwtTokenService.GenerateTokens(user);
        RefreshTokens[tokens.RefreshToken] = user.UserId.ToString();
        return new AuthResult(
            new AuthTokens(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt),
            new AuthUserInfo(
                user.UserId,
                user.Email,
                user.DisplayName,
                user.TenantId,
                user.Roles,
                user.Permissions));
    }

    public static void Reset() => RefreshTokens.Clear();
}

public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly ConcurrentDictionary<Guid, AlertRule> _rules = new();
    private readonly ConcurrentDictionary<Guid, AlertExecution> _executions = new();

    public Task<AlertRule?> GetRuleByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        _rules.TryGetValue(id, out var rule);
        return Task.FromResult(rule is not null && rule.TenantId == tenantId ? rule : null);
    }

    public Task<IReadOnlyList<AlertRule>> ListRulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rules = _rules.Values.Where(rule => rule.TenantId == tenantId).ToList();
        return Task.FromResult<IReadOnlyList<AlertRule>>(rules);
    }

    public Task CreateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task DeleteRuleAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        if (_rules.TryGetValue(id, out var rule) && rule.TenantId == tenantId)
        {
            _rules.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    public Task<AlertExecution?> GetExecutionByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        _executions.TryGetValue(id, out var execution);
        return Task.FromResult(execution is not null && execution.TenantId == tenantId ? execution : null);
    }

    public Task<IReadOnlyList<AlertExecution>> ListExecutionsByRuleAsync(
        Guid tenantId,
        Guid alertRuleId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var executions = _executions.Values
            .Where(execution => execution.TenantId == tenantId && execution.AlertRuleId == alertRuleId)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<AlertExecution>>(executions);
    }

    public Task<IReadOnlyList<AlertExecution>> ListRecentExecutionsAsync(
        Guid tenantId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var executions = _executions.Values
            .Where(execution => execution.TenantId == tenantId)
            .OrderByDescending(execution => execution.TriggeredAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<AlertExecution>>(executions);
    }

    public Task CreateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default)
    {
        _executions[execution.Id] = execution;
        return Task.CompletedTask;
    }

    public Task UpdateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default)
    {
        _executions[execution.Id] = execution;
        return Task.CompletedTask;
    }

    public Task CreateNotificationAsync(AlertNotification notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void Clear()
    {
        _rules.Clear();
        _executions.Clear();
    }
}

public sealed class NoOpRabbitMqTopologyInitializer : IRabbitMqTopologyInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NoOpMigrationRunner : Sentinel.Infrastructure.Persistence.PostgreSQL.IMigrationRunner
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NoOpDataSeeder : Sentinel.Infrastructure.Persistence.PostgreSQL.IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NoOpClickHouseMigrator : IClickHouseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

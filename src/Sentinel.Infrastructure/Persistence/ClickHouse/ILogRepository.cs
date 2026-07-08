using Sentinel.Domain.Observability;

namespace Sentinel.Infrastructure.Persistence.ClickHouse;

public sealed record LogSearchFilters(
    Guid TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Level,
    string? Service,
    string? Environment,
    string? Query,
    string? TraceId,
    int Limit = 100,
    int Offset = 0);

public sealed record LogSearchResult(
    IReadOnlyList<EnrichedLogRecord> Items,
    long TotalCount);

public interface ILogRepository
{
    Task BatchInsertRawAsync(IReadOnlyList<LogRecord> logs, CancellationToken cancellationToken = default);
    Task BatchInsertEnrichedAsync(IReadOnlyList<EnrichedLogRecord> logs, CancellationToken cancellationToken = default);
    Task<LogSearchResult> SearchAsync(LogSearchFilters filters, CancellationToken cancellationToken = default);
}

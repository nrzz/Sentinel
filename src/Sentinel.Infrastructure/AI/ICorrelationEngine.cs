using Sentinel.Domain.AI;

namespace Sentinel.Infrastructure.AI;

public interface ICorrelationEngine
{
    Task<CorrelationResult> CorrelateAsync(
        Guid tenantId,
        IReadOnlyList<string> signalIds,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default);
}

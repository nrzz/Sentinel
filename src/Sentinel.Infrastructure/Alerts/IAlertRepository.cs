using Sentinel.Domain.Alerts;

namespace Sentinel.Infrastructure.Alerts;

public interface IAlertRepository
{
    Task<AlertRule?> GetRuleByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertRule>> ListRulesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task CreateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default);
    Task UpdateRuleAsync(AlertRule rule, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<AlertExecution?> GetExecutionByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertExecution>> ListExecutionsByRuleAsync(
        Guid tenantId,
        Guid alertRuleId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertExecution>> ListRecentExecutionsAsync(
        Guid tenantId,
        int limit = 100,
        CancellationToken cancellationToken = default);
    Task CreateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default);
    Task UpdateExecutionAsync(AlertExecution execution, CancellationToken cancellationToken = default);
    Task CreateNotificationAsync(AlertNotification notification, CancellationToken cancellationToken = default);
}

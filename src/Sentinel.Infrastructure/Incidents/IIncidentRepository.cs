using Sentinel.Domain.Incidents;

namespace Sentinel.Infrastructure.Incidents;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task CreateAsync(Incident incident, CancellationToken cancellationToken = default);
    Task UpdateAsync(Incident incident, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentTimelineEntry>> ListTimelineAsync(
        Guid tenantId,
        Guid incidentId,
        CancellationToken cancellationToken = default);
    Task AddTimelineEntryAsync(IncidentTimelineEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncidentComment>> ListCommentsAsync(
        Guid tenantId,
        Guid incidentId,
        CancellationToken cancellationToken = default);
    Task<IncidentComment?> GetCommentByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task AddCommentAsync(IncidentComment comment, CancellationToken cancellationToken = default);
    Task UpdateCommentAsync(IncidentComment comment, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}

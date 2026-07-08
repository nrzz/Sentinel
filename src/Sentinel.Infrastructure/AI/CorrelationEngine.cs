using System.Text.Json;
using Microsoft.Extensions.Options;
using Sentinel.Domain.AI;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.Alerts;
using Sentinel.Infrastructure.Incidents;

namespace Sentinel.Infrastructure.AI;

public sealed class CorrelationEngine : ICorrelationEngine
{
    private readonly IAiProvider _provider;
    private readonly IPromptCatalog _promptCatalog;
    private readonly IAiAuditService _auditService;
    private readonly IAlertRepository _alertRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly AiOptions _options;

    public CorrelationEngine(
        IAiProvider provider,
        IPromptCatalog promptCatalog,
        IAiAuditService auditService,
        IAlertRepository alertRepository,
        IIncidentRepository incidentRepository,
        IOptions<AiOptions> options)
    {
        _provider = provider;
        _promptCatalog = promptCatalog;
        _auditService = auditService;
        _alertRepository = alertRepository;
        _incidentRepository = incidentRepository;
        _options = options.Value;
    }

    public async Task<CorrelationResult> CorrelateAsync(
        Guid tenantId,
        IReadOnlyList<string> signalIds,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var template = _promptCatalog.GetTemplate("correlate")
            ?? throw new InvalidOperationException("Correlation prompt template is not configured.");

        var matches = new List<CorrelationMatch>();
        var sources = new List<CorrelationSource>();
        var signalSummaries = new List<string>();

        foreach (var signalId in signalIds)
        {
            if (Guid.TryParse(signalId, out var guid))
            {
                var incident = await _incidentRepository.GetByIdAsync(tenantId, guid, cancellationToken);
                if (incident is not null)
                {
                    matches.Add(new CorrelationMatch
                    {
                        EntityType = "incident",
                        EntityId = incident.Id.ToString(),
                        Relationship = "primary",
                        Score = 0.9,
                        Description = incident.Title
                    });
                    sources.Add(new CorrelationSource
                    {
                        SourceType = "incident",
                        SourceId = incident.Id.ToString(),
                        Title = incident.Title,
                        Excerpt = incident.Description,
                        Citation = $"[source:incident:{incident.Id}] {incident.Title}"
                    });
                    signalSummaries.Add($"Incident {incident.Id}: {incident.Title} ({incident.Severity})");
                    continue;
                }

                var execution = await _alertRepository.GetExecutionByIdAsync(tenantId, guid, cancellationToken);
                if (execution is not null)
                {
                    matches.Add(new CorrelationMatch
                    {
                        EntityType = "alert_execution",
                        EntityId = execution.Id.ToString(),
                        Relationship = "triggered",
                        Score = 0.85,
                        Description = execution.Message
                    });
                    sources.Add(new CorrelationSource
                    {
                        SourceType = "alert_execution",
                        SourceId = execution.Id.ToString(),
                        Title = $"Alert execution {execution.Id}",
                        Excerpt = execution.Message,
                        Citation = $"[source:alert:{execution.Id}] {execution.Message}"
                    });
                    signalSummaries.Add($"Alert execution {execution.Id}: {execution.Message}");
                }
            }
            else
            {
                signalSummaries.Add($"External signal: {signalId}");
            }
        }

        var userPrompt = template.Render(new Dictionary<string, string>
        {
            ["signals"] = string.Join(Environment.NewLine, signalSummaries)
        });

        var request = new AiCompletionRequest(
            template.SystemPrompt,
            userPrompt,
            _options.Temperature,
            _options.MaxTokens);

        try
        {
            var response = await _provider.CompleteAsync(request, cancellationToken);
            var confidence = matches.Count > 0 ? Math.Min(0.95, 0.5 + (matches.Count * 0.15)) : 0.3;

            await _auditService.RecordAsync(
                tenantId,
                AiInteractionType.Correlate,
                template.Id,
                template.Version,
                JsonSerializer.Serialize(new { signalIds }),
                response,
                sources,
                correlationId,
                userId,
                cancellationToken);

            return new CorrelationResult
            {
                TenantId = tenantId,
                Summary = response.Content,
                ConfidenceScore = confidence,
                Matches = matches,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            await _auditService.RecordFailureAsync(
                tenantId,
                AiInteractionType.Correlate,
                _provider.ProviderName,
                _provider.ModelName,
                JsonSerializer.Serialize(new { signalIds }),
                ex.Message,
                correlationId,
                userId,
                cancellationToken);
            throw;
        }
    }
}

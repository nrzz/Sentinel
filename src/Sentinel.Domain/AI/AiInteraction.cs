using Sentinel.Domain.Common;

namespace Sentinel.Domain.AI;

public enum AiInteractionType
{
    Search = 0,
    Summarize = 1,
    Correlate = 2,
    Feedback = 3,
    Embedding = 4
}

public enum AiInteractionStatus
{
    Completed = 0,
    Failed = 1,
    Redacted = 2
}

public sealed class AiInteraction : Entity
{
    public Guid TenantId { get; private set; }
    public AiInteractionType InteractionType { get; private set; }
    public AiInteractionStatus Status { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string PromptTemplateId { get; private set; } = string.Empty;
    public string PromptTemplateVersion { get; private set; } = string.Empty;
    public string RequestPayload { get; private set; } = string.Empty;
    public string ResponsePayload { get; private set; } = string.Empty;
    public string? SourcesJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? UserId { get; private set; }
    public int? FeedbackRating { get; private set; }
    public string? FeedbackComment { get; private set; }
    public long LatencyMs { get; private set; }

    private AiInteraction() { }

    public static AiInteraction Create(
        Guid tenantId,
        AiInteractionType interactionType,
        string provider,
        string model,
        string promptTemplateId,
        string promptTemplateVersion,
        string requestPayload,
        string responsePayload,
        string? sourcesJson = null,
        string? correlationId = null,
        string? userId = null,
        long latencyMs = 0,
        bool wasRedacted = false)
    {
        return new AiInteraction
        {
            TenantId = tenantId,
            InteractionType = interactionType,
            Status = wasRedacted ? AiInteractionStatus.Redacted : AiInteractionStatus.Completed,
            Provider = provider,
            Model = model,
            PromptTemplateId = promptTemplateId,
            PromptTemplateVersion = promptTemplateVersion,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            SourcesJson = sourcesJson,
            CorrelationId = correlationId,
            UserId = userId,
            LatencyMs = latencyMs
        };
    }

    public static AiInteraction CreateFailed(
        Guid tenantId,
        AiInteractionType interactionType,
        string provider,
        string model,
        string requestPayload,
        string error,
        string? correlationId = null,
        string? userId = null)
    {
        return new AiInteraction
        {
            TenantId = tenantId,
            InteractionType = interactionType,
            Status = AiInteractionStatus.Failed,
            Provider = provider,
            Model = model,
            RequestPayload = requestPayload,
            ResponsePayload = error,
            CorrelationId = correlationId,
            UserId = userId
        };
    }

    public void AddFeedback(int rating, string? comment)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        }

        FeedbackRating = rating;
        FeedbackComment = comment?.Trim();
        Touch();
    }

    public static AiInteraction FromPersistence(
        Guid id,
        Guid tenantId,
        AiInteractionType interactionType,
        AiInteractionStatus status,
        string provider,
        string model,
        string promptTemplateId,
        string promptTemplateVersion,
        string requestPayload,
        string responsePayload,
        string? sourcesJson,
        string? correlationId,
        string? userId,
        int? feedbackRating,
        string? feedbackComment,
        long latencyMs,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            InteractionType = interactionType,
            Status = status,
            Provider = provider,
            Model = model,
            PromptTemplateId = promptTemplateId,
            PromptTemplateVersion = promptTemplateVersion,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            SourcesJson = sourcesJson,
            CorrelationId = correlationId,
            UserId = userId,
            FeedbackRating = feedbackRating,
            FeedbackComment = feedbackComment,
            LatencyMs = latencyMs,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}

using System.Text.Json;
using Npgsql;
using Sentinel.Domain.AI;
using Sentinel.Infrastructure.Persistence.PostgreSQL;

namespace Sentinel.Infrastructure.AI;

public interface IAiInteractionRepository
{
    Task<AiInteraction?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(AiInteraction interaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(AiInteraction interaction, CancellationToken cancellationToken = default);
}

public sealed class AiInteractionRepository : IAiInteractionRepository
{
    private readonly IPostgreSqlConnectionFactory _connectionFactory;

    public AiInteractionRepository(IPostgreSqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AiInteraction?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, interaction_type, status, provider, model, prompt_template_id,
                   prompt_template_version, request_payload, response_payload, sources_json,
                   correlation_id, user_id, feedback_rating, feedback_comment, latency_ms,
                   created_at, updated_at
            FROM ai_interactions
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapInteraction(reader) : null;
    }

    public async Task CreateAsync(AiInteraction interaction, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO ai_interactions (
                id, tenant_id, interaction_type, status, provider, model, prompt_template_id,
                prompt_template_version, request_payload, response_payload, sources_json,
                correlation_id, user_id, feedback_rating, feedback_comment, latency_ms,
                created_at, updated_at)
            VALUES (
                @id, @tenant_id, @interaction_type, @status, @provider, @model, @prompt_template_id,
                @prompt_template_version, @request_payload, @response_payload, @sources_json::jsonb,
                @correlation_id, @user_id, @feedback_rating, @feedback_comment, @latency_ms,
                @created_at, @updated_at)
            """,
            connection);

        AddParameters(command, interaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiInteraction interaction, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE ai_interactions SET
                feedback_rating = @feedback_rating,
                feedback_comment = @feedback_comment,
                updated_at = @updated_at
            WHERE tenant_id = @tenant_id AND id = @id
            """,
            connection);

        command.Parameters.AddWithValue("id", interaction.Id);
        command.Parameters.AddWithValue("tenant_id", interaction.TenantId);
        command.Parameters.AddWithValue("feedback_rating", (object?)interaction.FeedbackRating ?? DBNull.Value);
        command.Parameters.AddWithValue("feedback_comment", (object?)interaction.FeedbackComment ?? DBNull.Value);
        command.Parameters.AddWithValue("updated_at", interaction.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, AiInteraction interaction)
    {
        command.Parameters.AddWithValue("id", interaction.Id);
        command.Parameters.AddWithValue("tenant_id", interaction.TenantId);
        command.Parameters.AddWithValue("interaction_type", (int)interaction.InteractionType);
        command.Parameters.AddWithValue("status", (int)interaction.Status);
        command.Parameters.AddWithValue("provider", interaction.Provider);
        command.Parameters.AddWithValue("model", interaction.Model);
        command.Parameters.AddWithValue("prompt_template_id", interaction.PromptTemplateId);
        command.Parameters.AddWithValue("prompt_template_version", interaction.PromptTemplateVersion);
        command.Parameters.AddWithValue("request_payload", interaction.RequestPayload);
        command.Parameters.AddWithValue("response_payload", interaction.ResponsePayload);
        command.Parameters.AddWithValue("sources_json", (object?)interaction.SourcesJson ?? DBNull.Value);
        command.Parameters.AddWithValue("correlation_id", (object?)interaction.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("user_id", (object?)interaction.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("feedback_rating", (object?)interaction.FeedbackRating ?? DBNull.Value);
        command.Parameters.AddWithValue("feedback_comment", (object?)interaction.FeedbackComment ?? DBNull.Value);
        command.Parameters.AddWithValue("latency_ms", interaction.LatencyMs);
        command.Parameters.AddWithValue("created_at", interaction.CreatedAt);
        command.Parameters.AddWithValue("updated_at", interaction.UpdatedAt);
    }

    private static AiInteraction MapInteraction(NpgsqlDataReader reader) =>
        AiInteraction.FromPersistence(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("tenant_id")),
            (AiInteractionType)reader.GetInt32(reader.GetOrdinal("interaction_type")),
            (AiInteractionStatus)reader.GetInt32(reader.GetOrdinal("status")),
            reader.GetString(reader.GetOrdinal("provider")),
            reader.GetString(reader.GetOrdinal("model")),
            reader.GetString(reader.GetOrdinal("prompt_template_id")),
            reader.GetString(reader.GetOrdinal("prompt_template_version")),
            reader.GetString(reader.GetOrdinal("request_payload")),
            reader.GetString(reader.GetOrdinal("response_payload")),
            reader.IsDBNull(reader.GetOrdinal("sources_json"))
                ? null
                : reader.GetString(reader.GetOrdinal("sources_json")),
            reader.IsDBNull(reader.GetOrdinal("correlation_id"))
                ? null
                : reader.GetString(reader.GetOrdinal("correlation_id")),
            reader.IsDBNull(reader.GetOrdinal("user_id"))
                ? null
                : reader.GetString(reader.GetOrdinal("user_id")),
            reader.IsDBNull(reader.GetOrdinal("feedback_rating"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("feedback_rating")),
            reader.IsDBNull(reader.GetOrdinal("feedback_comment"))
                ? null
                : reader.GetString(reader.GetOrdinal("feedback_comment")),
            reader.GetInt64(reader.GetOrdinal("latency_ms")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")));
}

public interface IAiAuditService
{
    Task<AiInteraction> RecordAsync(
        Guid tenantId,
        AiInteractionType interactionType,
        string promptTemplateId,
        string promptTemplateVersion,
        string requestPayload,
        AiCompletionResponse response,
        IReadOnlyList<CorrelationSource>? sources = null,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<AiInteraction> RecordFailureAsync(
        Guid tenantId,
        AiInteractionType interactionType,
        string provider,
        string model,
        string requestPayload,
        string error,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default);
}

public sealed class AiAuditService : IAiAuditService
{
    private readonly IAiInteractionRepository _repository;
    private readonly ISecretRedactor _redactor;

    public AiAuditService(IAiInteractionRepository repository, ISecretRedactor redactor)
    {
        _repository = repository;
        _redactor = redactor;
    }

    public async Task<AiInteraction> RecordAsync(
        Guid tenantId,
        AiInteractionType interactionType,
        string promptTemplateId,
        string promptTemplateVersion,
        string requestPayload,
        AiCompletionResponse response,
        IReadOnlyList<CorrelationSource>? sources = null,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var (redactedRequest, requestRedacted) = _redactor.Redact(requestPayload);
        var (redactedResponse, responseRedacted) = _redactor.Redact(response.Content);
        var sourcesJson = sources is { Count: > 0 }
            ? JsonSerializer.Serialize(sources)
            : null;

        var interaction = AiInteraction.Create(
            tenantId,
            interactionType,
            response.Provider,
            response.Model,
            promptTemplateId,
            promptTemplateVersion,
            redactedRequest,
            redactedResponse,
            sourcesJson,
            correlationId,
            userId,
            response.LatencyMs,
            requestRedacted || responseRedacted);

        await _repository.CreateAsync(interaction, cancellationToken);
        return interaction;
    }

    public async Task<AiInteraction> RecordFailureAsync(
        Guid tenantId,
        AiInteractionType interactionType,
        string provider,
        string model,
        string requestPayload,
        string error,
        string? correlationId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var (redactedRequest, _) = _redactor.Redact(requestPayload);
        var (redactedError, _) = _redactor.Redact(error);

        var interaction = AiInteraction.CreateFailed(
            tenantId,
            interactionType,
            provider,
            model,
            redactedRequest,
            redactedError,
            correlationId,
            userId);

        await _repository.CreateAsync(interaction, cancellationToken);
        return interaction;
    }
}

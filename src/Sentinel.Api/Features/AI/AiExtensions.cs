using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sentinel.Api;
using Sentinel.Domain.AI;
using Sentinel.Domain.Configuration;
using Sentinel.Infrastructure.AI;

namespace Sentinel.Api.Features.AI;

public static class AiExtensions
{
    public static IServiceCollection AddAiFeatures(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        services.AddHttpClient<OllamaAiProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AiOptions>>().Value.Ollama;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<OpenAiProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AiOptions>>().Value.OpenAi;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddSingleton<IAiProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            return options.DefaultProvider.Equals("openai", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<OpenAiProvider>()
                : sp.GetRequiredService<OllamaAiProvider>();
        });

        services.AddSingleton<ISecretRedactor, SecretRedactor>();
        services.AddSingleton<IPromptCatalog, PromptCatalog>();
        services.AddScoped<IAiInteractionRepository, AiInteractionRepository>();
        services.AddScoped<IAiAuditService, AiAuditService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<ICorrelationEngine, CorrelationEngine>();

        return services;
    }

    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("AI")
            .RequireAuthorization();

        group.MapPost("/search", Search);
        group.MapPost("/summarize", Summarize);
        group.MapPost("/correlate", Correlate);
        group.MapPost("/feedback", SubmitFeedback);

        return app;
    }

    private static async Task<IResult> Search(
        AiSearchRequest request,
        HttpContext context,
        [FromServices] IRagService ragService,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        if (request.Documents is { Count: > 0 })
        {
            await ragService.IndexDocumentsAsync(request.Documents, cancellationToken);
        }

        var result = await ragService.SearchAsync(
            tenantId,
            request.Question,
            context.GetCorrelationId(),
            context.GetUserId(),
            cancellationToken);

        return Results.Ok(new AiSearchResponse(
            result.Answer,
            result.Sources,
            result.PromptTemplateId,
            result.PromptTemplateVersion));
    }

    private static async Task<IResult> Summarize(
        AiSummarizeRequest request,
        HttpContext context,
        [FromServices] IAiProvider provider,
        [FromServices] IPromptCatalog promptCatalog,
        [FromServices] IAiAuditService auditService,
        [FromServices] IOptions<AiOptions> options,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var template = promptCatalog.GetTemplate("summarize")
            ?? throw new InvalidOperationException("Summarize prompt template is not configured.");

        var userPrompt = template.Render(new Dictionary<string, string>
        {
            ["content_type"] = request.ContentType,
            ["content"] = request.Content
        });

        var aiRequest = new AiCompletionRequest(
            template.SystemPrompt,
            userPrompt,
            options.Value.Temperature,
            options.Value.MaxTokens);

        try
        {
            var response = await provider.CompleteAsync(aiRequest, cancellationToken);
            var interaction = await auditService.RecordAsync(
                tenantId,
                AiInteractionType.Summarize,
                template.Id,
                template.Version,
                JsonSerializer.Serialize(new { request.ContentType, request.Content }),
                response,
                correlationId: context.GetCorrelationId(),
                userId: context.GetUserId(),
                cancellationToken: cancellationToken);

            return Results.Ok(new AiSummarizeResponse(
                response.Content,
                interaction.Id,
                template.Id,
                template.Version));
        }
        catch (Exception ex)
        {
            await auditService.RecordFailureAsync(
                tenantId,
                AiInteractionType.Summarize,
                provider.ProviderName,
                provider.ModelName,
                JsonSerializer.Serialize(new { request.ContentType, request.Content }),
                ex.Message,
                context.GetCorrelationId(),
                context.GetUserId(),
                cancellationToken);

            return Results.Problem(
                title: "AI summarization failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> Correlate(
        AiCorrelateRequest request,
        HttpContext context,
        [FromServices] ICorrelationEngine correlationEngine,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        if (request.SignalIds.Count == 0)
        {
            return Results.BadRequest(new { error = "At least one signal ID is required." });
        }

        var result = await correlationEngine.CorrelateAsync(
            tenantId,
            request.SignalIds,
            context.GetCorrelationId(),
            context.GetUserId(),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> SubmitFeedback(
        AiFeedbackRequest request,
        HttpContext context,
        [FromServices] IAiInteractionRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantId = context.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var interaction = await repository.GetByIdAsync(tenantId, request.InteractionId, cancellationToken);
        if (interaction is null)
        {
            return Results.NotFound();
        }

        interaction.AddFeedback(request.Rating, request.Comment);
        await repository.UpdateAsync(interaction, cancellationToken);

        return Results.Ok(new { interaction.Id, interaction.FeedbackRating, interaction.FeedbackComment });
    }
}

public sealed record AiSearchRequest(string Question, IReadOnlyList<RagDocument>? Documents);

public sealed record AiSearchResponse(
    string Answer,
    IReadOnlyList<CorrelationSource> Sources,
    string PromptTemplateId,
    string PromptTemplateVersion);

public sealed record AiSummarizeRequest(string ContentType, string Content);

public sealed record AiSummarizeResponse(
    string Summary,
    Guid InteractionId,
    string PromptTemplateId,
    string PromptTemplateVersion);

public sealed record AiCorrelateRequest(IReadOnlyList<string> SignalIds);

public sealed record AiFeedbackRequest(Guid InteractionId, int Rating, string? Comment);

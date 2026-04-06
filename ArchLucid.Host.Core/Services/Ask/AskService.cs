using System.Text.Json;

using ArchiForge.AgentRuntime;
using ArchiForge.Host.Core.Ask;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Conversation;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Models;
using ArchiForge.Retrieval.Queries;

namespace ArchiForge.Host.Core.Services.Ask;

/// <summary>
/// <see cref="IAskService"/> implementation: conversation thread + structured JSON context + optional retrieval + LLM JSON answer shape.
/// </summary>
/// <remarks>
/// Retrieval and post-answer indexing failures are logged and do not fail the request. LLM failures return a short fallback <see cref="AskResponse"/>.
/// </remarks>
public sealed class AskService(
    IAuthorityQueryService query,
    IProvenanceQueryService provenanceQuery,
    IComparisonService comparison,
    IAgentCompletionClient llm,
    IConversationService conversationService,
    IRetrievalQueryService retrievalQuery,
    IRetrievalDocumentBuilder retrievalDocumentBuilder,
    IRetrievalIndexingService retrievalIndexingService,
    ILogger<AskService> logger) : IAskService
{
    private const int HistoryTake = 40;

    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions MetadataWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string ArchitectSystemPrompt =
        "You are a senior enterprise architect. " +
        "Use ONLY the provided architecture context JSON, conversation history, and retrieved evidence. " +
        "Be precise and technical. Reference decisions by Title and SelectedOption (and DecisionId when helpful). " +
        "Do not invent services, findings, artifacts, or costs not present in the supplied materials. " +
        "If something is unknown from the supplied data, say so. " +
        "Prefer retrieved evidence when answering specifics that are not in the structured context. " +
        "Use prior conversation only when it helps interpret follow-up questions (e.g. \"that decision\", \"the storage choice\"). " +
        "Respond with a single JSON object only (no markdown fences), keys: " +
        "answer (string), referencedDecisions (array of strings), referencedFindings (array of strings), " +
        "referencedArtifacts (array of strings — use provenance graph node labels where Type suggests an artifact, or empty array).";

    /// <inheritdoc />
    /// <remarks>
    /// Loads manifest for <see cref="AskRequest.RunId"/> or thread default; builds comparison when both base and target run ids resolve.
    /// Appends user message before LLM call and assistant message after; indexes the turn best-effort.
    /// </remarks>
    public async Task<AskResponse> AskAsync(AskRequest request, ScopeContext scope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException("Question is required.", nameof(request));

        ConversationThread thread = await conversationService.GetOrCreateThreadAsync(
            request.ThreadId,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request.RunId,
            request.BaseRunId,
            request.TargetRunId,
            ct);

        Guid? effectiveRunId = request.RunId ?? thread.RunId;
        Guid? effectiveBaseRunId = request.BaseRunId ?? thread.BaseRunId;
        Guid? effectiveTargetRunId = request.TargetRunId ?? thread.TargetRunId;

        if (!effectiveRunId.HasValue)
        
            throw new InvalidOperationException(
                "No run is anchored. Provide runId on the first message, or use a thread that already has a run.");
        

        await conversationService.AppendUserMessageAsync(thread.ThreadId, request.Question.Trim(), ct);

        IReadOnlyList<ConversationMessage> historyWindow = await conversationService.GetHistoryAsync(thread.ThreadId, HistoryTake, ct);
        IReadOnlyList<ConversationMessage> priorMessages = TrimCurrentUserTurn(historyWindow, request.Question.Trim());
        string historyText = BuildConversationHistory(priorMessages);

        RunDetailDto? detail = await query.GetRunDetailAsync(scope, effectiveRunId.Value, ct);
        if (detail?.GoldenManifest is null)
        
            throw new InvalidOperationException(
                "Run not found or has no GoldenManifest for the current scope.");
        

        GoldenManifest? manifest = detail.GoldenManifest;
        GraphViewModel? graph = await provenanceQuery.GetFullGraphAsync(scope, effectiveRunId.Value, ct);

        ComparisonResult? comparisonResult = null;
        if (effectiveBaseRunId.HasValue && effectiveTargetRunId.HasValue)
        {
            RunDetailDto? baseRun = await query.GetRunDetailAsync(scope, effectiveBaseRunId.Value, ct);
            RunDetailDto? targetRun = await query.GetRunDetailAsync(scope, effectiveTargetRunId.Value, ct);
            if (baseRun?.GoldenManifest is not null && targetRun?.GoldenManifest is not null)
                comparisonResult = comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        }

        object context = ContextBuilder.BuildContext(manifest, graph, comparisonResult);
        string contextJson = JsonSerializer.Serialize(context, JsonWrite);

        IReadOnlyList<RetrievalHit> retrievalHits = [];
        try
        {
            retrievalHits = await retrievalQuery.SearchAsync(
                new RetrievalQuery
                {
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    RunId = null,
                    ManifestId = null,
                    QueryText = request.Question.Trim(),
                    TopK = 8
                },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Retrieval search failed for Ask; continuing without retrieved evidence.");
        }

        string retrievalContext = BuildRetrievalContext(retrievalHits);

        string userPrompt =
            "Conversation History:\n" +
            (string.IsNullOrWhiteSpace(historyText) ? "(none)\n" : historyText + "\n") +
            "\nStructured Context:\n" +
            contextJson +
            "\n\nRetrieved Evidence:\n" +
            (string.IsNullOrWhiteSpace(retrievalContext) ? "(none)\n" : retrievalContext + "\n") +
            "\nUser Question:\n" +
            request.Question.Trim();

        string? raw;
        try
        {
            raw = await llm.CompleteJsonAsync(ArchitectSystemPrompt, userPrompt, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM completion failed for Ask (ThreadId={ThreadId}); returning fallback response.", thread.ThreadId);
            return new AskResponse
            {
                ThreadId = thread.ThreadId,
                Answer =
                    "The assistant could not be reached. Summarize from context manually or retry. " +
                    (manifest is not null
                        ? "Context included " + manifest.Decisions.Count + " decision(s)."
                        : "No manifest was loaded for this run.")
            };
        }

        raw = UnwrapJsonFence(raw);
        LlmAskShape? parsed = TryDeserialize(raw);

        AskResponse response;
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Answer))
        
            response = new AskResponse
            {
                ThreadId = thread.ThreadId,
                Answer = string.IsNullOrWhiteSpace(raw)
                    ? "No answer produced."
                    : raw.Trim(),
                ReferencedDecisions = [],
                ReferencedFindings = [],
                ReferencedArtifacts = []
            };
        
        else
        
            response = new AskResponse
            {
                ThreadId = thread.ThreadId,
                Answer = parsed.Answer.Trim(),
                ReferencedDecisions = NormalizeList(parsed.ReferencedDecisions),
                ReferencedFindings = NormalizeList(parsed.ReferencedFindings),
                ReferencedArtifacts = NormalizeList(parsed.ReferencedArtifacts)
            };
        

        string metadataJson = JsonSerializer.Serialize(
            new
            {
                response.ReferencedDecisions,
                response.ReferencedFindings,
                response.ReferencedArtifacts
            },
            MetadataWrite);

        await conversationService.AppendAssistantMessageAsync(
            thread.ThreadId,
            response.Answer,
            metadataJson,
            ct);

        try
        {
            DateTime now = DateTime.UtcNow;
            List<ConversationMessage> conversationTurn =
            [
                new()
                {
                    MessageId = Guid.NewGuid(),
                    ThreadId = thread.ThreadId,
                    Role = ConversationMessageRole.User,
                    Content = request.Question.Trim(),
                    CreatedUtc = now,
                    MetadataJson = "{}"
                },

                new()
                {
                    MessageId = Guid.NewGuid(),
                    ThreadId = thread.ThreadId,
                    Role = ConversationMessageRole.Assistant,
                    Content = response.Answer,
                    CreatedUtc = now,
                    MetadataJson = metadataJson
                }
            ];

            IReadOnlyList<RetrievalDocument> convDocs = retrievalDocumentBuilder.BuildForConversation(
                scope.TenantId,
                scope.WorkspaceId,
                scope.ProjectId,
                effectiveRunId,
                conversationTurn);

            await retrievalIndexingService.IndexDocumentsAsync(convDocs, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index Ask conversation turn for retrieval.");
        }

        return response;
    }

    private static string BuildRetrievalContext(IReadOnlyList<RetrievalHit> hits)
    {
        if (hits.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            hits.Select((h, i) =>
                $"[{i + 1}] {h.SourceType} / {h.Title}{Environment.NewLine}{h.Text}"));
    }

    /// <summary>Exclude the just-appended user message from the history block (it is repeated as User Question).</summary>
    private static IReadOnlyList<ConversationMessage> TrimCurrentUserTurn(
        IReadOnlyList<ConversationMessage> messages,
        string question)
    {
        if (messages.Count == 0)
            return messages;

        ConversationMessage last = messages[^1];
        if (last.Role == ConversationMessageRole.User &&
            string.Equals(last.Content.Trim(), question, StringComparison.Ordinal))
            return messages.Take(messages.Count - 1).ToList();

        return messages;
    }

    private static string BuildConversationHistory(IReadOnlyList<ConversationMessage> messages)
    {
        if (messages.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            messages.Select(m => $"{m.Role}: {m.Content}"));
    }

    private static List<string> NormalizeList(IEnumerable<string>? items) =>
        items?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string? UnwrapJsonFence(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;
        string s = raw.Trim();
        if (!s.StartsWith("```", StringComparison.Ordinal))
            return s;
        int firstNl = s.IndexOf('\n');
        if (firstNl > 0)
            s = s[(firstNl + 1)..].Trim();
        int end = s.LastIndexOf("```", StringComparison.Ordinal);
        if (end > 0)
            s = s[..end].Trim();
        return s;
    }

    private LlmAskShape? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<LlmAskShape>(json, JsonRead);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize LLM Ask response as JSON; falling back to raw text.");
            return null;
        }
    }

    private sealed class LlmAskShape
    {
        public string? Answer { get; init; }
        public List<string>? ReferencedDecisions { get; init; }
        public List<string>? ReferencedFindings { get; init; }
        public List<string>? ReferencedArtifacts { get; init; }
    }
}

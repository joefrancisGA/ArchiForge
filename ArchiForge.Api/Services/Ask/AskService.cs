using System.Text.Json;
using ArchiForge.AgentRuntime;
using ArchiForge.Api.Ask;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Conversation;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;

namespace ArchiForge.Api.Services.Ask;

public sealed class AskService(
    IAuthorityQueryService query,
    IProvenanceQueryService provenanceQuery,
    IComparisonService comparison,
    IAgentCompletionClient llm,
    IConversationService conversationService) : IAskService
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
        "Use ONLY the provided architecture context JSON and conversation history. " +
        "Be precise and technical. Reference decisions by Title and SelectedOption (and DecisionId when helpful). " +
        "Do not invent services, findings, artifacts, or costs not present in the context or prior turns. " +
        "If something is unknown from the supplied data, say so. " +
        "Use prior conversation only when it helps interpret follow-up questions (e.g. \"that decision\", \"the storage choice\"). " +
        "Respond with a single JSON object only (no markdown fences), keys: " +
        "answer (string), referencedDecisions (array of strings), referencedFindings (array of strings), " +
        "referencedArtifacts (array of strings — use provenance graph node labels where Type suggests an artifact, or empty array).";

    public async Task<AskResponse> AskAsync(AskRequest request, ScopeContext scope, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException("Question is required.", nameof(request));

        var thread = await conversationService.GetOrCreateThreadAsync(
            request.ThreadId,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            request.RunId,
            request.BaseRunId,
            request.TargetRunId,
            ct);

        var effectiveRunId = request.RunId ?? thread.RunId;
        var effectiveBaseRunId = request.BaseRunId ?? thread.BaseRunId;
        var effectiveTargetRunId = request.TargetRunId ?? thread.TargetRunId;

        if (!effectiveRunId.HasValue)
        {
            throw new InvalidOperationException(
                "No run is anchored. Provide runId on the first message, or use a thread that already has a run.");
        }

        await conversationService.AppendUserMessageAsync(thread.ThreadId, request.Question.Trim(), ct);

        var historyWindow = await conversationService.GetHistoryAsync(thread.ThreadId, HistoryTake, ct);
        var priorMessages = TrimCurrentUserTurn(historyWindow, request.Question.Trim());
        var historyText = BuildConversationHistory(priorMessages);

        var detail = await query.GetRunDetailAsync(scope, effectiveRunId.Value, ct);
        if (detail?.GoldenManifest is null)
        {
            throw new InvalidOperationException(
                "Run not found or has no GoldenManifest for the current scope.");
        }

        var manifest = detail.GoldenManifest;
        var graph = await provenanceQuery.GetFullGraphAsync(scope, effectiveRunId.Value, ct);

        ComparisonResult? comparisonResult = null;
        if (effectiveBaseRunId.HasValue && effectiveTargetRunId.HasValue)
        {
            var baseRun = await query.GetRunDetailAsync(scope, effectiveBaseRunId.Value, ct);
            var targetRun = await query.GetRunDetailAsync(scope, effectiveTargetRunId.Value, ct);
            if (baseRun?.GoldenManifest is not null && targetRun?.GoldenManifest is not null)
                comparisonResult = comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        }

        var context = ContextBuilder.BuildContext(manifest, graph, comparisonResult);
        var contextJson = JsonSerializer.Serialize(context, JsonWrite);

        var userPrompt =
            "Conversation History:\n" +
            (string.IsNullOrWhiteSpace(historyText) ? "(none)\n" : historyText + "\n") +
            "\nArchitecture Context:\n" +
            contextJson +
            "\n\nUser Question:\n" +
            request.Question.Trim();

        string? raw;
        try
        {
            raw = await llm.CompleteJsonAsync(ArchitectSystemPrompt, userPrompt, ct);
        }
        catch
        {
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
        var parsed = TryDeserialize(raw);

        AskResponse response;
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Answer))
        {
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
        }
        else
        {
            response = new AskResponse
            {
                ThreadId = thread.ThreadId,
                Answer = parsed.Answer.Trim(),
                ReferencedDecisions = NormalizeList(parsed.ReferencedDecisions),
                ReferencedFindings = NormalizeList(parsed.ReferencedFindings),
                ReferencedArtifacts = NormalizeList(parsed.ReferencedArtifacts)
            };
        }

        var metadataJson = JsonSerializer.Serialize(
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

        return response;
    }

    /// <summary>Exclude the just-appended user message from the history block (it is repeated as User Question).</summary>
    private static IReadOnlyList<ConversationMessage> TrimCurrentUserTurn(
        IReadOnlyList<ConversationMessage> messages,
        string question)
    {
        if (messages.Count == 0)
            return messages;

        var last = messages[messages.Count - 1];
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
        var s = raw.Trim();
        if (!s.StartsWith("```", StringComparison.Ordinal))
            return s;
        var firstNl = s.IndexOf('\n');
        if (firstNl > 0)
            s = s[(firstNl + 1)..].Trim();
        var end = s.LastIndexOf("```", StringComparison.Ordinal);
        if (end > 0)
            s = s[..end].Trim();
        return s;
    }

    private static LlmAskShape? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<LlmAskShape>(json, JsonRead);
        }
        catch
        {
            return null;
        }
    }

    private sealed class LlmAskShape
    {
        public string? Answer { get; set; }
        public List<string>? ReferencedDecisions { get; set; }
        public List<string>? ReferencedFindings { get; set; }
        public List<string>? ReferencedArtifacts { get; set; }
    }
}

using System.Text.Json;
using ArchiForge.AgentRuntime;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;

namespace ArchiForge.Api.Services.Ask;

public sealed class AskService(
    IAuthorityQueryService query,
    IProvenanceQueryService provenanceQuery,
    IComparisonService comparison,
    IAgentCompletionClient llm) : IAskService
{
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

    private const string ArchitectSystemPrompt =
        "You are a senior enterprise architect. Answer the user's question using ONLY the provided context JSON. " +
        "Be precise and technical. Reference decisions by Title and SelectedOption (and DecisionId when helpful). " +
        "Do not invent services, findings, artifacts, or costs not present in the context. " +
        "If the context does not contain enough information, say what is unknown. " +
        "Respond with a single JSON object only (no markdown fences), keys: " +
        "answer (string), referencedDecisions (array of strings), referencedFindings (array of strings), " +
        "referencedArtifacts (array of strings — use provenance graph node labels where Type suggests an artifact, or empty array).";

    public async Task<AskResponse> AskAsync(AskRequest request, ScopeContext scope, CancellationToken ct)
    {
        if (!request.RunId.HasValue)
            throw new ArgumentException("RunId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException("Question is required.", nameof(request));

        var detail = await query.GetRunDetailAsync(scope, request.RunId.Value, ct);
        if (detail?.GoldenManifest is null)
            throw new InvalidOperationException("Run not found or has no GoldenManifest for the current scope.");

        var graph = await provenanceQuery.GetFullGraphAsync(scope, request.RunId.Value, ct);

        ComparisonResult? comparisonResult = null;
        if (request.BaseRunId.HasValue && request.TargetRunId.HasValue)
        {
            var baseRun = await query.GetRunDetailAsync(scope, request.BaseRunId.Value, ct);
            var targetRun = await query.GetRunDetailAsync(scope, request.TargetRunId.Value, ct);
            if (baseRun?.GoldenManifest is not null && targetRun?.GoldenManifest is not null)
                comparisonResult = comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        }

        var context = ContextBuilder.BuildContext(detail.GoldenManifest, graph, comparisonResult);
        var contextJson = JsonSerializer.Serialize(context, JsonWrite);

        var userPrompt =
            "Context:\n" +
            contextJson +
            "\n\nQuestion:\n" +
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
                Answer =
                    "The assistant could not be reached. Summarize from context manually or retry. " +
                    "Context included " + detail.GoldenManifest.Decisions.Count + " decision(s)."
            };
        }

        raw = UnwrapJsonFence(raw);
        var parsed = TryDeserialize(raw);

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Answer))
        {
            return new AskResponse
            {
                Answer = string.IsNullOrWhiteSpace(raw)
                    ? "No answer produced."
                    : raw.Trim(),
                ReferencedDecisions = [],
                ReferencedFindings = [],
                ReferencedArtifacts = []
            };
        }

        return new AskResponse
        {
            Answer = parsed.Answer.Trim(),
            ReferencedDecisions = NormalizeList(parsed.ReferencedDecisions),
            ReferencedFindings = NormalizeList(parsed.ReferencedFindings),
            ReferencedArtifacts = NormalizeList(parsed.ReferencedArtifacts)
        };
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

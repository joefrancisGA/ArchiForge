using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
/// Verifies that trace <c>ParsedResultJson</c> is a JSON object and contains expected top-level keys for a serialized <see cref="AgentResult"/>.
/// </summary>
public sealed class AgentOutputEvaluator : IAgentOutputEvaluator
{
    /// <summary>
    /// Keys produced by <see cref="JsonSerializerDefaults.Web"/> for <see cref="AgentResult"/> (handlers use this for trace JSON).
    /// </summary>
    private static readonly string[] SharedAgentResultKeys =
    [
        "resultId",
        "taskId",
        "runId",
        "agentType",
        "claims",
        "evidenceRefs",
        "confidence",
        "findings",
        "proposedChanges",
        "createdUtc",
    ];

    /// <inheritdoc />
    public AgentOutputEvaluationScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);

        string[] expected = GetExpectedKeys(agentType);

        if (string.IsNullOrWhiteSpace(parsedResultJson))
        {
            return new AgentOutputEvaluationScore
            {
                TraceId = traceId,
                AgentType = agentType,
                StructuralCompletenessRatio = 0.0,
                IsJsonParseFailure = false,
                MissingKeys = expected,
            };
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(parsedResultJson);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return BuildScore(traceId, agentType, 0.0, true, expected);
            }

            HashSet<string> present = CollectPropertyNames(doc.RootElement);
            List<string> missing = expected.Where(k => !present.Contains(k)).ToList();
            int hitCount = expected.Length - missing.Count;
            double ratio = expected.Length == 0 ? 1.0 : (double)hitCount / expected.Length;

            return new AgentOutputEvaluationScore
            {
                TraceId = traceId,
                AgentType = agentType,
                StructuralCompletenessRatio = ratio,
                IsJsonParseFailure = false,
                MissingKeys = missing,
            };
        }
        catch (JsonException)
        {
            return BuildScore(traceId, agentType, 0.0, true, expected);
        }
    }

    private static AgentOutputEvaluationScore BuildScore(
        string traceId,
        AgentType agentType,
        double ratio,
        bool parseFailure,
        IReadOnlyList<string> expected)
    {
        return new AgentOutputEvaluationScore
        {
            TraceId = traceId,
            AgentType = agentType,
            StructuralCompletenessRatio = ratio,
            IsJsonParseFailure = parseFailure,
            MissingKeys = parseFailure ? expected : Array.Empty<string>(),
        };
    }

    private static HashSet<string> CollectPropertyNames(JsonElement root)
    {
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (JsonProperty p in root.EnumerateObject())
        {
            _ = names.Add(p.Name);
        }

        return names;
    }

    private static string[] GetExpectedKeys(AgentType agentType)
    {
        // Same contract shape for every agent; kept as a switch for future per-type expectations.
        return agentType switch
        {
            AgentType.Topology => SharedAgentResultKeys,
            AgentType.Cost => SharedAgentResultKeys,
            AgentType.Compliance => SharedAgentResultKeys,
            AgentType.Critic => SharedAgentResultKeys,
            _ => SharedAgentResultKeys,
        };
    }
}

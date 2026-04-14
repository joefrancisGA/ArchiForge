using System.Text.Json;

using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
/// Deterministic JSON inspection scoring claim evidence and finding completeness.
/// </summary>
public sealed class AgentOutputSemanticEvaluator : IAgentOutputSemanticEvaluator
{
    private const int MinDescriptionLength = 10;
    private const int MinRecommendationLength = 5;

    public AgentOutputSemanticScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);

        if (string.IsNullOrWhiteSpace(parsedResultJson))
        {
            return BuildZeroScore(traceId, agentType);
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(parsedResultJson);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return BuildZeroScore(traceId, agentType);
            }

            (double claimsRatio, int emptyClaims) = EvaluateClaims(doc.RootElement);
            (double findingsRatio, int incompleteFindings) = EvaluateFindings(doc.RootElement);

            double overall = ComputeOverallScore(claimsRatio, findingsRatio, doc.RootElement);

            return new AgentOutputSemanticScore
            {
                TraceId = traceId,
                AgentType = agentType,
                ClaimsQualityRatio = claimsRatio,
                FindingsQualityRatio = findingsRatio,
                EmptyClaimCount = emptyClaims,
                IncompleteFindingCount = incompleteFindings,
                OverallSemanticScore = overall,
            };
        }
        catch (JsonException)
        {
            return BuildZeroScore(traceId, agentType);
        }
    }

    private static (double ratio, int emptyCount) EvaluateClaims(JsonElement root)
    {
        if (!root.TryGetProperty("claims", out JsonElement claimsElement) || claimsElement.ValueKind != JsonValueKind.Array)
        {
            return (0.0, 0);
        }

        int total = 0;
        int withEvidence = 0;

        foreach (JsonElement claim in claimsElement.EnumerateArray())
        {
            total++;

            if (claim.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            bool hasEvidenceRefs = claim.TryGetProperty("evidenceRefs", out JsonElement refs)
                                   && refs.ValueKind == JsonValueKind.Array
                                   && refs.GetArrayLength() > 0;

            bool hasEvidence = claim.TryGetProperty("evidence", out JsonElement ev)
                               && ev.ValueKind == JsonValueKind.String
                               && ev.GetString()?.Length > 0;

            if (hasEvidenceRefs || hasEvidence)
            {
                withEvidence++;
            }
        }

        if (total == 0)
        {
            return (0.0, 0);
        }

        return ((double)withEvidence / total, total - withEvidence);
    }

    private static (double ratio, int incompleteCount) EvaluateFindings(JsonElement root)
    {
        if (!root.TryGetProperty("findings", out JsonElement findingsElement) || findingsElement.ValueKind != JsonValueKind.Array)
        {
            return (0.0, 0);
        }

        int total = 0;
        int complete = 0;

        foreach (JsonElement finding in findingsElement.EnumerateArray())
        {
            total++;

            if (finding.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            bool hasSeverity = finding.TryGetProperty("severity", out JsonElement sev)
                               && sev.ValueKind == JsonValueKind.String
                               && (sev.GetString()?.Length ?? 0) > 0;

            bool hasDescription = finding.TryGetProperty("description", out JsonElement desc)
                                  && desc.ValueKind == JsonValueKind.String
                                  && (desc.GetString()?.Length ?? 0) > MinDescriptionLength;

            bool hasRecommendation = finding.TryGetProperty("recommendation", out JsonElement rec)
                                     && rec.ValueKind == JsonValueKind.String
                                     && (rec.GetString()?.Length ?? 0) > MinRecommendationLength;

            if (hasSeverity && hasDescription && hasRecommendation)
            {
                complete++;
            }
        }

        if (total == 0)
        {
            return (0.0, 0);
        }

        return ((double)complete / total, total - complete);
    }

    private static double ComputeOverallScore(double claimsRatio, double findingsRatio, JsonElement root)
    {
        bool hasClaims = root.TryGetProperty("claims", out JsonElement c)
                         && c.ValueKind == JsonValueKind.Array
                         && c.GetArrayLength() > 0;

        bool hasFindings = root.TryGetProperty("findings", out JsonElement f)
                           && f.ValueKind == JsonValueKind.Array
                           && f.GetArrayLength() > 0;

        if (!hasClaims && !hasFindings)
        {
            return 0.0;
        }

        if (hasClaims && !hasFindings)
        {
            return claimsRatio;
        }

        if (!hasClaims && hasFindings)
        {
            return findingsRatio;
        }

        return (claimsRatio * 0.4) + (findingsRatio * 0.6);
    }

    private static AgentOutputSemanticScore BuildZeroScore(string traceId, AgentType agentType)
    {
        return new AgentOutputSemanticScore
        {
            TraceId = traceId,
            AgentType = agentType,
            ClaimsQualityRatio = 0.0,
            FindingsQualityRatio = 0.0,
            EmptyClaimCount = 0,
            IncompleteFindingCount = 0,
            OverallSemanticScore = 0.0,
        };
    }
}

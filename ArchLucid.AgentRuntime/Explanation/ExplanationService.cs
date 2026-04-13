using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;
using ArchLucid.Provenance;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Explanation;

/// <summary>
/// Structured signals first, then LLM narrative (JSON). Falls back to signal-only text if the model fails.
/// </summary>
/// <inheritdoc cref="IExplanationService"/>
public sealed class ExplanationService(
    IAgentCompletionClient completionClient,
    IDeterministicExplanationService deterministic,
    IOptions<ExplanationServiceOptions> explanationOptions,
    ILogger<ExplanationService> logger) : IExplanationService
{
    private const string ArchitectSystemPrompt =
        "You are a senior enterprise architect. Be concise but authoritative. " +
        "Ground every statement in the facts provided; do not invent services or decisions not listed.";

    /// <inheritdoc />
    public async Task<ComparisonExplanationResult> ExplainComparisonAsync(
        ComparisonResult comparison,
        CancellationToken ct)
    {
        List<string> majorChanges = deterministic.ExtractMajorChanges(comparison);
        string securityBlock = deterministic.FormatSecurityChanges(comparison);
        string costBlock = deterministic.FormatCostChanges(comparison);
        string topologyBlock = deterministic.FormatTopologyChanges(comparison);
        string reqBlock = deterministic.FormatRequirementChanges(comparison);

        string userPrompt =
            "Explain the following architecture changes between a BASE run and a TARGET run.\n\n" +
            "## Summary counts\n" +
            $"- Decision deltas: {comparison.DecisionChanges.Count}\n" +
            $"- Requirement deltas: {comparison.RequirementChanges.Count}\n" +
            $"- Security deltas: {comparison.SecurityChanges.Count}\n" +
            $"- Topology deltas: {comparison.TopologyChanges.Count}\n" +
            $"- Cost deltas: {comparison.CostChanges.Count}\n\n" +
            "## Decision / choice changes\n" + string.Join("\n", majorChanges) + "\n\n" +
            "## Requirement changes\n" + reqBlock + "\n\n" +
            "## Security changes\n" + securityBlock + "\n\n" +
            "## Topology changes\n" + topologyBlock + "\n\n" +
            "## Cost changes\n" + costBlock + "\n\n" +
            "## Highlight strings\n" + string.Join("\n", comparison.SummaryHighlights.Select(h => "- " + h)) +
            "\n\n" +
            "Respond with a single JSON object only (no markdown fences), keys:\n" +
            "highLevelSummary (string), keyTradeoffs (array of strings), narrative (string, 2-4 short paragraphs).";

        string? json = await TryCompleteJsonAsync(userPrompt, ct);

        return deterministic.BuildComparisonExplanation(comparison, majorChanges, json);
    }

    /// <inheritdoc />
    public async Task<ExplanationResult> ExplainRunAsync(
        GoldenManifest manifest,
        DecisionProvenanceGraph? provenance,
        CancellationToken ct)
    {
        List<string> keyDrivers = deterministic.ExtractRunKeyDrivers(manifest, provenance);
        List<string> risks = deterministic.ExtractRiskImplications(manifest);
        List<string> costs = deterministic.ExtractCostImplications(manifest);
        List<string> compliance = deterministic.ExtractComplianceImplications(manifest);

        string userPrompt =
            "Explain this architecture run for stakeholders.\n\n" +
            "## Manifest summary (source of truth)\n" +
            (string.IsNullOrWhiteSpace(manifest.Metadata.Summary)
                ? "(none)\n"
                : manifest.Metadata.Summary + "\n") +
            "\n## Key drivers (must be reflected in your reasoning)\n" +
            string.Join("\n", keyDrivers.Select(x => "- " + x)) +
            "\n\n## Risks / issues (from manifest)\n" +
            string.Join("\n", risks.Select(x => "- " + x)) +
            "\n\n## Cost signals\n" +
            string.Join("\n", costs.Select(x => "- " + x)) +
            "\n\n## Compliance signals\n" +
            string.Join("\n", compliance.Select(x => "- " + x)) +
            "\n\n## Provenance\n" +
            deterministic.FormatProvenanceSummary(provenance) +
            "\n\nRespond with a single JSON object only (no markdown fences), matching this schema (camelCase keys):\n" +
            "- schemaVersion: number (use 1)\n" +
            "- reasoning: string (required) — 2–4 paragraphs referencing the bullets above\n" +
            "- evidenceRefs: string[] — optional provenance or decision IDs you cite\n" +
            "- confidence: number between 0 and 1, or omit if unknown\n" +
            "- alternativesConsidered: string[] — optional\n" +
            "- caveats: string[] — optional limitations\n" +
            "Example: {\"schemaVersion\":1,\"reasoning\":\"...\",\"evidenceRefs\":[\"dec-1\"],\"confidence\":0.72}\n" +
            "If you cannot follow the schema, respond with plain prose only (no JSON); the system will still accept it.";

        string? json = await TryCompleteJsonAsync(userPrompt, ct);
        string rawStored = json ?? string.Empty;

        return FinalizeRunExplanation(
            deterministic.BuildRunExplanationFromLlmPayload(
                manifest,
                keyDrivers,
                risks,
                costs,
                compliance,
                rawStored));
    }

    private ExplanationResult FinalizeRunExplanation(ExplanationResult result)
    {
        result.Confidence = result.Structured?.Confidence;
        result.Provenance = BuildProvenance();

        return result;
    }

    private ExplanationProvenance BuildProvenance()
    {
        ExplanationServiceOptions o = explanationOptions.Value;
        LlmProviderDescriptor d = completionClient.Descriptor;
        string agentType = string.IsNullOrWhiteSpace(o.AgentType) ? "run-explanation" : o.AgentType.Trim();
        string modelId = string.IsNullOrWhiteSpace(d.ModelId) ? "unknown" : d.ModelId.Trim();

        return new ExplanationProvenance(
            AgentType: agentType,
            ModelId: modelId,
            PromptTemplateId: string.IsNullOrWhiteSpace(o.PromptTemplateId) ? null : o.PromptTemplateId.Trim(),
            PromptTemplateVersion: string.IsNullOrWhiteSpace(o.PromptTemplateVersion)
                ? null
                : o.PromptTemplateVersion.Trim(),
            PromptContentHash: string.IsNullOrWhiteSpace(o.PromptContentHash) ? null : o.PromptContentHash.Trim());
    }

    private async Task<string?> TryCompleteJsonAsync(string userPrompt, CancellationToken ct)
    {
        try
        {
            string raw = await completionClient.CompleteJsonAsync(ArchitectSystemPrompt, userPrompt, ct);

            return UnwrapJsonFence(raw);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM completion failed in ExplanationService; falling back to heuristic response.");

            return null;
        }
    }

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
}

using System.Text.Json;

using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;
using ArchLucid.Provenance;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Explanation;

/// <summary>
/// Structured signals first, then LLM narrative (JSON). Falls back to signal-only text if the model fails.
/// </summary>
/// <inheritdoc cref="IExplanationService"/>
public sealed class ExplanationService(
    IAgentCompletionClient completionClient,
    ILogger<ExplanationService> logger) : IExplanationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private const string ArchitectSystemPrompt =
        "You are a senior enterprise architect. Be concise but authoritative. " +
        "Ground every statement in the facts provided; do not invent services or decisions not listed.";

    /// <inheritdoc />
    public async Task<ComparisonExplanationResult> ExplainComparisonAsync(
        ComparisonResult comparison,
        CancellationToken ct)
    {
        List<string> majorChanges = ExtractMajorChanges(comparison);
        string securityBlock = FormatSecurityChanges(comparison);
        string costBlock = FormatCostChanges(comparison);
        string topologyBlock = FormatTopologyChanges(comparison);
        string reqBlock = FormatRequirementChanges(comparison);

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
        LlmComparisonJson? parsed = TryDeserialize<LlmComparisonJson>(json);

        return new ComparisonExplanationResult
        {
            HighLevelSummary = !string.IsNullOrWhiteSpace(parsed?.HighLevelSummary)
                ? parsed.HighLevelSummary.Trim()
                : BuildComparisonHeuristicSummary(comparison),
            MajorChanges = majorChanges,
            KeyTradeoffs = parsed?.KeyTradeoffs?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .ToList() ?? [],
            Narrative = !string.IsNullOrWhiteSpace(parsed?.Narrative)
                ? parsed.Narrative.Trim()
                : BuildComparisonNarrativeFallback(comparison, majorChanges)
        };
    }

    /// <inheritdoc />
    public async Task<ExplanationResult> ExplainRunAsync(
        GoldenManifest manifest,
        DecisionProvenanceGraph? provenance,
        CancellationToken ct)
    {
        List<string> keyDrivers = ExtractRunKeyDrivers(manifest, provenance);
        List<string> risks = ExtractRiskImplications(manifest);
        List<string> costs = ExtractCostImplications(manifest);
        List<string> compliance = ExtractComplianceImplications(manifest);

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
            FormatProvenanceSummary(provenance) +
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

        return BuildExplanationResultFromLlmResponse(
            manifest,
            keyDrivers,
            risks,
            costs,
            compliance,
            rawStored);
    }

    private ExplanationResult BuildExplanationResultFromLlmResponse(
        GoldenManifest manifest,
        List<string> keyDrivers,
        List<string> risks,
        List<string> costs,
        List<string> compliance,
        string rawStored)
    {
        string heuristicSummary = HeuristicRunSummary(manifest);
        string narrativeFallback = BuildRunNarrativeFallback(manifest, keyDrivers, risks);

        ExplanationResult result = new()
        {
            RawText = rawStored,
            KeyDrivers = keyDrivers,
            RiskImplications = risks,
            CostImplications = costs,
            ComplianceImplications = compliance,
        };

        if (StructuredExplanationParser.TryNormalizeStructuredJson(rawStored, out StructuredExplanation? structured))
        {
            result.Structured = structured;
            result.DetailedNarrative = structured.Reasoning.Trim();
            result.Summary = SummarizeFromReasoning(structured.Reasoning, heuristicSummary);
            return result;
        }

        LlmRunJsonDto? legacy = TryDeserialize<LlmRunJsonDto>(rawStored);

        if (legacy is not null
            && (!string.IsNullOrWhiteSpace(legacy.Summary) || !string.IsNullOrWhiteSpace(legacy.DetailedNarrative)))
        {
            string summary = !string.IsNullOrWhiteSpace(legacy.Summary)
                ? legacy.Summary.Trim()
                : heuristicSummary;
            string narrative = !string.IsNullOrWhiteSpace(legacy.DetailedNarrative)
                ? legacy.DetailedNarrative.Trim()
                : (!string.IsNullOrWhiteSpace(legacy.Summary) ? legacy.Summary.Trim() : narrativeFallback);

            result.Summary = summary;
            result.DetailedNarrative = narrative;
            result.Structured = new StructuredExplanation
            {
                Reasoning = narrative,
                SchemaVersion = 1,
                EvidenceRefs = [],
            };
            return result;
        }

        if (!string.IsNullOrWhiteSpace(rawStored))
        {
            if (IsProbablyJsonObject(rawStored))
            {
                result.Summary = heuristicSummary;
                result.DetailedNarrative = narrativeFallback;
                result.Structured = new StructuredExplanation
                {
                    Reasoning = narrativeFallback,
                    SchemaVersion = 1,
                    EvidenceRefs = [],
                };
                return result;
            }

            StructuredExplanation envelope = StructuredExplanationParser.Parse(rawStored);
            result.Structured = envelope;
            result.DetailedNarrative = string.IsNullOrWhiteSpace(envelope.Reasoning)
                ? narrativeFallback
                : envelope.Reasoning.Trim();
            result.Summary = SummarizeFromReasoning(result.DetailedNarrative, heuristicSummary);
            return result;
        }

        result.Summary = heuristicSummary;
        result.DetailedNarrative = narrativeFallback;
        result.Structured = new StructuredExplanation
        {
            Reasoning = narrativeFallback,
            SchemaVersion = 1,
            EvidenceRefs = [],
        };
        return result;
    }

    private static bool IsProbablyJsonObject(string raw)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(raw);

            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string HeuristicRunSummary(GoldenManifest manifest) =>
        string.IsNullOrWhiteSpace(manifest.Metadata.Summary)
            ? $"Run {manifest.RunId} manifest ({manifest.Decisions.Count} decisions, {manifest.UnresolvedIssues.Items.Count} open issues)."
            : manifest.Metadata.Summary.Trim();

    private static string SummarizeFromReasoning(string reasoning, string heuristicSummary)
    {
        string r = reasoning.Trim();

        if (r.Length == 0)
            return heuristicSummary;

        int idx = r.IndexOf("\n\n", StringComparison.Ordinal);

        string first = idx > 0 ? r[..idx] : r;

        const int maxLen = 500;

        if (first.Length > maxLen)
            return first[..maxLen].TrimEnd() + "…";

        return first;
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

    private T? TryDeserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize LLM Explanation response as {Type}; falling back to heuristic.", typeof(T).Name);
            return null;
        }
    }

    private static List<string> ExtractMajorChanges(ComparisonResult c)
    {
        List<string> list = [];
        foreach (DecisionDelta d in c.DecisionChanges)
        
            if (d.ChangeType == "Modified")
            
                list.Add(
                    $"Decision '{d.DecisionKey}' changed from '{d.BaseValue ?? "—"}' to '{d.TargetValue ?? "—"}'.");
            
            else if (d.ChangeType == "Added")
            
                list.Add($"Decision '{d.DecisionKey}' added (selected: '{d.TargetValue ?? "—"}').");
            
            else if (d.ChangeType == "Removed")
            
                list.Add($"Decision '{d.DecisionKey}' removed (was '{d.BaseValue ?? "—"}').");
            
        

        list.AddRange(c.RequirementChanges.Take(30).Select(r => $"Requirement '{r.RequirementName}': {r.ChangeType}."));

        return list;
    }

    private static string FormatRequirementChanges(ComparisonResult c) =>
        c.RequirementChanges.Count == 0
            ? "(none)"
            : string.Join("\n", c.RequirementChanges.Select(r => $"- {r.RequirementName}: {r.ChangeType}"));

    private static string FormatSecurityChanges(ComparisonResult c) =>
        c.SecurityChanges.Count == 0
            ? "(none)"
            : string.Join("\n",
                c.SecurityChanges.Select(s =>
                    $"- {s.ControlName}: {s.BaseStatus ?? "—"} → {s.TargetStatus ?? "—"}"));

    private static string FormatTopologyChanges(ComparisonResult c) =>
        c.TopologyChanges.Count == 0
            ? "(none)"
            : string.Join("\n", c.TopologyChanges.Select(t => $"- {t.Resource} ({t.ChangeType})"));

    private static string FormatCostChanges(ComparisonResult c) =>
        c.CostChanges.Count == 0
            ? "(none)"
            : string.Join("\n",
                c.CostChanges.Select(x =>
                    $"- Max monthly: {x.BaseCost?.ToString("0.00") ?? "—"} → {x.TargetCost?.ToString("0.00") ?? "—"}"));

    private static string BuildComparisonHeuristicSummary(ComparisonResult c)
    {
        List<string> parts = [];
        if (c.DecisionChanges.Count > 0)
            parts.Add($"{c.DecisionChanges.Count} decision change(s)");
        if (c.RequirementChanges.Count > 0)
            parts.Add($"{c.RequirementChanges.Count} requirement change(s)");
        if (c.SecurityChanges.Count > 0)
            parts.Add($"{c.SecurityChanges.Count} security delta(s)");
        if (c.TopologyChanges.Count > 0)
            parts.Add($"{c.TopologyChanges.Count} topology resource change(s)");
        if (c.CostChanges.Count > 0)
            parts.Add("cost posture changed");
        return parts.Count == 0
            ? "No material differences detected between manifests."
            : "Between runs: " + string.Join("; ", parts) + ".";
    }

    private static string BuildComparisonNarrativeFallback(ComparisonResult c, List<string> majorChanges)
    {
        List<string> lines =
        [
            "The target run differs from the base run in the areas summarized below.",
            string.Join(" ", c.SummaryHighlights)
        ];
        lines.AddRange(majorChanges.Take(15));
        return string.Join("\n\n", lines.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static List<string> ExtractRunKeyDrivers(GoldenManifest m, DecisionProvenanceGraph? g)
    {
        List<string> list = m.Decisions.Take(25).Select(d => $"{d.Category}: {d.Title} → {d.SelectedOption}").ToList();

        if (m.Topology.Resources.Count > 0)
            list.Add($"{m.Topology.Resources.Count} topology resource(s) recorded.");

        if (m.Compliance.Gaps.Count > 0)
            list.Add($"{m.Compliance.Gaps.Count} compliance gap(s).");

        if (g is null)
            return list;

        Dictionary<ProvenanceNodeType, int> byType = g.Nodes.GroupBy(n => n.Type).ToDictionary(x => x.Key, x => x.Count());
        list.Add(
            $"Provenance graph: {g.Nodes.Count} node(s), {g.Edges.Count} edge(s); " +
            string.Join(", ", byType.Select(kv => $"{kv.Key}={kv.Value}")));

        return list;
    }

    private static List<string> ExtractRiskImplications(GoldenManifest m)
    {
        List<string> list = m.UnresolvedIssues.Items.Take(20).Select(i => $"[{i.Severity}] {i.Title}: {i.Description}").ToList();
        list.AddRange(m.Warnings.Take(10).Select(w => $"Warning: {w}"));

        if (list.Count == 0)
            list.Add("No unresolved issues recorded.");

        return list;
    }

    private static List<string> ExtractCostImplications(GoldenManifest m)
    {
        List<string> list =
        [
            m.Cost.MaxMonthlyCost.HasValue
                ? $"Max monthly cost: {m.Cost.MaxMonthlyCost.Value:0.00}"
                : "Max monthly cost not specified."
        ];

        list.AddRange(m.Cost.CostRisks.Take(10).Select(r => $"Cost risk: {r}"));

        return list;
    }

    private static List<string> ExtractComplianceImplications(GoldenManifest m)
    {
        List<string> list = m.Compliance.Gaps.Take(15).Select(g => $"Compliance gap: {g}").ToList();

        if (m.Compliance.Controls.Count > 0)
            list.Insert(0, $"{m.Compliance.Controls.Count} compliance control(s) evaluated.");

        if (list.Count == 0)
            list.Add("No compliance gaps listed.");

        return list;
    }

    private static string FormatProvenanceSummary(DecisionProvenanceGraph? g)
    {
        return g is null ? "No provenance graph supplied." : $"Nodes: {g.Nodes.Count}, Edges: {g.Edges.Count}. RunId on graph: {g.RunId}.";
    }

    private static string BuildRunNarrativeFallback(
        GoldenManifest m,
        List<string> drivers,
        List<string> risks)
    {
        return string.Join("\n\n",
            new[]
            {
                $"This run ({m.RunId}) reflects {m.Decisions.Count} recorded architecture decision(s).",
                "Key drivers:\n" + string.Join("\n", drivers.Take(12).Select(x => "- " + x)),
                "Risk / issue context:\n" + string.Join("\n", risks.Take(8).Select(x => "- " + x))
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    [UsedImplicitly]
    private sealed class LlmComparisonJson
    {
        [UsedImplicitly]
        public string? HighLevelSummary { get; }
        [UsedImplicitly]
        public List<string>? KeyTradeoffs { get; }
        [UsedImplicitly]
        public string? Narrative { get; }
    }

    [UsedImplicitly]
    private sealed class LlmRunJsonDto
    {
        [UsedImplicitly]
        public string? Summary { get; set; }

        [UsedImplicitly]
        public string? DetailedNarrative { get; set; }
    }
}

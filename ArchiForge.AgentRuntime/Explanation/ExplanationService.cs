using System.Text.Json;

using ArchiForge.Core.Comparison;
using ArchiForge.Core.Explanation;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArchiForge.AgentRuntime.Explanation;

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
        var majorChanges = ExtractMajorChanges(comparison);
        var securityBlock = FormatSecurityChanges(comparison);
        var costBlock = FormatCostChanges(comparison);
        var topologyBlock = FormatTopologyChanges(comparison);
        var reqBlock = FormatRequirementChanges(comparison);

        var userPrompt =
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

        var json = await TryCompleteJsonAsync(userPrompt, ct);
        var parsed = TryDeserialize<LlmComparisonJson>(json);

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
        var keyDrivers = ExtractRunKeyDrivers(manifest, provenance);
        var risks = ExtractRiskImplications(manifest);
        var costs = ExtractCostImplications(manifest);
        var compliance = ExtractComplianceImplications(manifest);

        var userPrompt =
            "Explain this architecture run for stakeholders.\n\n" +
            "## Manifest summary (source of truth)\n" +
            (string.IsNullOrWhiteSpace(manifest.Metadata.Summary)
                ? "(none)\n"
                : manifest.Metadata.Summary + "\n") +
            "\n## Key drivers (must be reflected in your narrative)\n" +
            string.Join("\n", keyDrivers.Select(x => "- " + x)) +
            "\n\n## Risks / issues (from manifest)\n" +
            string.Join("\n", risks.Select(x => "- " + x)) +
            "\n\n## Cost signals\n" +
            string.Join("\n", costs.Select(x => "- " + x)) +
            "\n\n## Compliance signals\n" +
            string.Join("\n", compliance.Select(x => "- " + x)) +
            "\n\n## Provenance\n" +
            FormatProvenanceSummary(provenance) +
            "\n\nRespond with a single JSON object only (no markdown fences), keys:\n" +
            "summary (one paragraph), detailedNarrative (2-4 paragraphs referencing the bullets above).";

        var json = await TryCompleteJsonAsync(userPrompt, ct);
        var parsed = TryDeserialize<LlmRunJson>(json);

        return new ExplanationResult
        {
            Summary = !string.IsNullOrWhiteSpace(parsed?.Summary)
                ? parsed.Summary.Trim()
                : string.IsNullOrWhiteSpace(manifest.Metadata.Summary)
                    ? $"Run {manifest.RunId} manifest ({manifest.Decisions.Count} decisions, {manifest.UnresolvedIssues.Items.Count} open issues)."
                    : manifest.Metadata.Summary.Trim(),
            KeyDrivers = keyDrivers,
            RiskImplications = risks,
            CostImplications = costs,
            ComplianceImplications = compliance,
            DetailedNarrative = !string.IsNullOrWhiteSpace(parsed?.DetailedNarrative)
                ? parsed.DetailedNarrative.Trim()
                : BuildRunNarrativeFallback(manifest, keyDrivers, risks)
        };
    }

    private async Task<string?> TryCompleteJsonAsync(string userPrompt, CancellationToken ct)
    {
        try
        {
            var raw = await completionClient.CompleteJsonAsync(ArchitectSystemPrompt, userPrompt, ct);
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
        var list = new List<string>();
        foreach (var d in c.DecisionChanges)
        {
            if (d.ChangeType == "Modified")
            {
                list.Add(
                    $"Decision '{d.DecisionKey}' changed from '{d.BaseValue ?? "—"}' to '{d.TargetValue ?? "—"}'.");
            }
            else if (d.ChangeType == "Added")
            {
                list.Add($"Decision '{d.DecisionKey}' added (selected: '{d.TargetValue ?? "—"}').");
            }
            else if (d.ChangeType == "Removed")
            {
                list.Add($"Decision '{d.DecisionKey}' removed (was '{d.BaseValue ?? "—"}').");
            }
        }

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
        var parts = new List<string>();
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
        var lines = new List<string>
        {
            "The target run differs from the base run in the areas summarized below.",
            string.Join(" ", c.SummaryHighlights)
        };
        lines.AddRange(majorChanges.Take(15));
        return string.Join("\n\n", lines.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static List<string> ExtractRunKeyDrivers(GoldenManifest m, DecisionProvenanceGraph? g)
    {
        var list = m.Decisions.Take(25).Select(d => $"{d.Category}: {d.Title} → {d.SelectedOption}").ToList();

        if (m.Topology.Resources.Count > 0)
            list.Add($"{m.Topology.Resources.Count} topology resource(s) recorded.");

        if (m.Compliance.Gaps.Count > 0)
            list.Add($"{m.Compliance.Gaps.Count} compliance gap(s).");

        if (g is null)
            return list;

        var byType = g.Nodes.GroupBy(n => n.Type).ToDictionary(x => x.Key, x => x.Count());
        list.Add(
            $"Provenance graph: {g.Nodes.Count} node(s), {g.Edges.Count} edge(s); " +
            string.Join(", ", byType.Select(kv => $"{kv.Key}={kv.Value}")));

        return list;
    }

    private static List<string> ExtractRiskImplications(GoldenManifest m)
    {
        var list = m.UnresolvedIssues.Items.Take(20).Select(i => $"[{i.Severity}] {i.Title}: {i.Description}").ToList();
        list.AddRange(m.Warnings.Take(10).Select(w => $"Warning: {w}"));

        if (list.Count == 0)
            list.Add("No unresolved issues recorded.");

        return list;
    }

    private static List<string> ExtractCostImplications(GoldenManifest m)
    {
        var list = new List<string>
        {
            m.Cost.MaxMonthlyCost.HasValue
                ? $"Max monthly cost: {m.Cost.MaxMonthlyCost.Value:0.00}"
                : "Max monthly cost not specified."
        };

        list.AddRange(m.Cost.CostRisks.Take(10).Select(r => $"Cost risk: {r}"));

        return list;
    }

    private static List<string> ExtractComplianceImplications(GoldenManifest m)
    {
        var list = m.Compliance.Gaps.Take(15).Select(g => $"Compliance gap: {g}").ToList();

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
        public string? HighLevelSummary
        {
            get;
        }
        [UsedImplicitly]
        public List<string>? KeyTradeoffs
        {
            get;
        }
        [UsedImplicitly]
        public string? Narrative
        {
            get;
        }
    }

    [UsedImplicitly]
    private sealed class LlmRunJson
    {
        [UsedImplicitly]
        public string? Summary
        {
            get;
        }
        [UsedImplicitly]
        public string? DetailedNarrative
        {
            get;
        }
    }
}

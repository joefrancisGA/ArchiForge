using System.Text.Json;

using ArchLucid.Core.Comparison;
using ArchLucid.Core.Explanation;
using ArchLucid.Decisioning.Models;
using ArchLucid.Provenance;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Explanation;

/// <inheritdoc cref="IDeterministicExplanationService"/>
public sealed class DeterministicExplanationService(ILogger<DeterministicExplanationService> logger)
    : IDeterministicExplanationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    public ComparisonExplanationResult BuildComparisonExplanation(
        ComparisonResult comparison,
        List<string> majorChanges,
        string? llmJson)
    {
        LlmComparisonJson? parsed = TryDeserialize<LlmComparisonJson>(llmJson);

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
    public ExplanationResult BuildRunExplanationFromLlmPayload(
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

    /// <inheritdoc />
    public List<string> ExtractMajorChanges(ComparisonResult c)
    {
        List<string> list = [];

        foreach (DecisionDelta d in c.DecisionChanges)
        {
            if (d.ChangeType == "Modified")

                list.Add(
                    $"Decision '{d.DecisionKey}' changed from '{d.BaseValue ?? "—"}' to '{d.TargetValue ?? "—"}'.");

            else if (d.ChangeType == "Added")

                list.Add($"Decision '{d.DecisionKey}' added (selected: '{d.TargetValue ?? "—"}').");

            else if (d.ChangeType == "Removed")

                list.Add($"Decision '{d.DecisionKey}' removed (was '{d.BaseValue ?? "—"}').");
        }

        list.AddRange(c.RequirementChanges.Take(30).Select(r => $"Requirement '{r.RequirementName}': {r.ChangeType}."));

        return list;
    }

    /// <inheritdoc />
    public string FormatRequirementChanges(ComparisonResult c) =>
        c.RequirementChanges.Count == 0
            ? "(none)"
            : string.Join("\n", c.RequirementChanges.Select(r => $"- {r.RequirementName}: {r.ChangeType}"));

    /// <inheritdoc />
    public string FormatSecurityChanges(ComparisonResult c) =>
        c.SecurityChanges.Count == 0
            ? "(none)"
            : string.Join("\n",
                c.SecurityChanges.Select(s =>
                    $"- {s.ControlName}: {s.BaseStatus ?? "—"} → {s.TargetStatus ?? "—"}"));

    /// <inheritdoc />
    public string FormatTopologyChanges(ComparisonResult c) =>
        c.TopologyChanges.Count == 0
            ? "(none)"
            : string.Join("\n", c.TopologyChanges.Select(t => $"- {t.Resource} ({t.ChangeType})"));

    /// <inheritdoc />
    public string FormatCostChanges(ComparisonResult c) =>
        c.CostChanges.Count == 0
            ? "(none)"
            : string.Join("\n",
                c.CostChanges.Select(x =>
                    $"- Max monthly: {x.BaseCost?.ToString("0.00") ?? "—"} → {x.TargetCost?.ToString("0.00") ?? "—"}"));

    /// <inheritdoc />
    public List<string> ExtractRunKeyDrivers(GoldenManifest m, DecisionProvenanceGraph? g)
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

    /// <inheritdoc />
    public List<string> ExtractRiskImplications(GoldenManifest m)
    {
        List<string> list = m.UnresolvedIssues.Items.Take(20).Select(i => $"[{i.Severity}] {i.Title}: {i.Description}").ToList();
        list.AddRange(m.Warnings.Take(10).Select(w => $"Warning: {w}"));

        if (list.Count == 0)
            list.Add("No unresolved issues recorded.");

        return list;
    }

    /// <inheritdoc />
    public List<string> ExtractCostImplications(GoldenManifest m)
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

    /// <inheritdoc />
    public List<string> ExtractComplianceImplications(GoldenManifest m)
    {
        List<string> list = m.Compliance.Gaps.Take(15).Select(g => $"Compliance gap: {g}").ToList();

        if (m.Compliance.Controls.Count > 0)
            list.Insert(0, $"{m.Compliance.Controls.Count} compliance control(s) evaluated.");

        if (list.Count == 0)
            list.Add("No compliance gaps listed.");

        return list;
    }

    /// <inheritdoc />
    public string FormatProvenanceSummary(DecisionProvenanceGraph? g) =>
        g is null ? "No provenance graph supplied." : $"Nodes: {g.Nodes.Count}, Edges: {g.Edges.Count}. RunId on graph: {g.RunId}.";

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

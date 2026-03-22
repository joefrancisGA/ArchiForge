using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Provenance;

namespace ArchiForge.Api.Services.Ask;

/// <summary>Builds a JSON-serializable view of manifest + provenance + optional comparison for RAG-style prompting.</summary>
public static class ContextBuilder
{
    private const int MaxGraphNodes = 120;
    private const int MaxGraphEdges = 200;

    public static object BuildContext(
        GoldenManifest? manifest,
        GraphViewModel? provenance,
        ComparisonResult? comparison)
    {
        if (manifest is null)
        {
            return new
            {
                ManifestAvailable = false,
                Note =
                    "No GoldenManifest is anchored for this turn. Rely on conversation history; " +
                    "if the user asks for specifics not in history, say the manifest context is unavailable.",
                ComparisonSummary = BuildComparisonSummary(comparison),
                Changes = comparison?.DecisionChanges.Select(c => new
                {
                    c.DecisionKey,
                    c.ChangeType,
                    c.BaseValue,
                    c.TargetValue
                })
            };
        }

        return new
        {
            ManifestAvailable = true,
            manifest.RunId,
            manifest.ManifestId,
            manifest.Metadata.Summary,
            Decisions = manifest.Decisions.Select(d => new
            {
                d.DecisionId,
                d.Category,
                d.Title,
                d.SelectedOption,
                Rationale = string.IsNullOrWhiteSpace(d.Rationale) ? null : d.Rationale,
                d.SupportingFindingIds
            }),
            Findings = manifest.Provenance.SourceFindingIds,
            manifest.Provenance.SourceGraphNodeIds,
            manifest.Provenance.AppliedRuleIds,
            ComplianceGaps = manifest.Compliance.Gaps,
            Cost = new
            {
                manifest.Cost.MaxMonthlyCost, manifest.Cost.CostRisks
            },
            UnresolvedIssues = manifest.UnresolvedIssues.Items.Take(25).Select(i => new
            {
                i.Severity,
                i.Title,
                i.Description
            }),
            Changes = comparison?.DecisionChanges.Select(c => new
            {
                c.DecisionKey,
                c.ChangeType,
                c.BaseValue,
                c.TargetValue
            }),
            ComparisonSummary = BuildComparisonSummary(comparison),
            ProvenanceGraph = provenance is null
                ? null
                : new
                {
                    NodeCount = provenance.Nodes.Count,
                    EdgeCount = provenance.Edges.Count,
                    Nodes = provenance.Nodes.Take(MaxGraphNodes)
                        .Select(n => new { n.Id, n.Label, n.Type }),
                    Edges = provenance.Edges.Take(MaxGraphEdges)
                        .Select(e => new { e.Source, e.Target, e.Type })
                }
        };
    }

    private static object? BuildComparisonSummary(ComparisonResult? comparison) =>
        comparison is null
            ? null
            : new
            {
                comparison.BaseRunId,
                comparison.TargetRunId,
                comparison.SummaryHighlights,
                RequirementChangeCount = comparison.RequirementChanges.Count,
                SecurityChangeCount = comparison.SecurityChanges.Count,
                TopologyChangeCount = comparison.TopologyChanges.Count,
                CostChangeCount = comparison.CostChanges.Count,
                SampleRequirementChanges = comparison.RequirementChanges.Take(20)
                    .Select(r => new { r.RequirementName, r.ChangeType }),
                SampleCostChanges = comparison.CostChanges.Take(10)
                    .Select(x => new { x.BaseCost, x.TargetCost })
            };
}

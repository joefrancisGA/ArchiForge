using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Advisory.Analysis;

/// <summary>
/// Default <see cref="IImprovementSignalAnalyzer"/> implementation driven by manifest gaps and <see cref="ComparisonResult"/> deltas.
/// </summary>
public sealed class ImprovementSignalAnalyzer : IImprovementSignalAnalyzer
{
    /// <inheritdoc />
    /// <remarks>
    /// Currently does not read individual findings from <paramref name="findingsSnapshot"/>; extension points use manifest and comparison only.
    /// </remarks>
    public IReadOnlyList<ImprovementSignal> Analyze(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        ComparisonResult? comparison = null)
    {
        _ = findingsSnapshot;

        var signals = new List<ImprovementSignal>();

        AnalyzeRequirementSignals(manifest, signals);
        AnalyzeSecuritySignals(manifest, signals);
        AnalyzeComplianceSignals(manifest, signals);
        AnalyzeTopologySignals(manifest, signals);
        AnalyzeCostSignals(manifest, signals);
        AnalyzeUnresolvedIssueSignals(manifest, signals);

        if (comparison is not null)
            AnalyzeComparisonSignals(comparison, signals);

        return signals;
    }

    private static void AnalyzeRequirementSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var uncovered in manifest.Requirements.Uncovered)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = "UncoveredRequirement",
                Category = "Requirement",
                Title = $"Requirement not covered: {uncovered.RequirementName}",
                Description = string.IsNullOrWhiteSpace(uncovered.RequirementText)
                    ? uncovered.RequirementName
                    : uncovered.RequirementText,
                Severity = "High",
                FindingIds = uncovered.SupportingFindingIds.ToList()
            });
        }
    }

    private static void AnalyzeSecuritySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var gap in manifest.Security.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = "SecurityGap",
                Category = "Security",
                Title = "Security protection gap",
                Description = gap,
                Severity = "High"
            });
        }
    }

    private static void AnalyzeComplianceSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var gap in manifest.Compliance.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = "ComplianceGap",
                Category = "Compliance",
                Title = "Compliance gap detected",
                Description = gap,
                Severity = "High"
            });
        }
    }

    private static void AnalyzeTopologySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var gap in manifest.Topology.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = "TopologyGap",
                Category = "Topology",
                Title = "Topology coverage gap",
                Description = gap,
                Severity = "Medium"
            });
        }
    }

    private static void AnalyzeCostSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var risk in manifest.Cost.CostRisks)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = "CostRisk",
                Category = "Cost",
                Title = "Cost risk detected",
                Description = risk,
                Severity = "Medium"
            });
        }
    }

    private static void AnalyzeUnresolvedIssueSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (var issue in manifest.UnresolvedIssues.Items)
        {
            var sev = string.IsNullOrWhiteSpace(issue.Severity) ? "Medium" : issue.Severity;
            signals.Add(new ImprovementSignal
            {
                SignalType = "UnresolvedIssue",
                Category = "Risk",
                Title = issue.Title,
                Description = issue.Description,
                Severity = string.Equals(sev, "Critical", StringComparison.OrdinalIgnoreCase) ? "Critical" : sev,
                FindingIds = issue.SupportingFindingIds.ToList()
            });
        }
    }

    private static void AnalyzeComparisonSignals(ComparisonResult comparison, List<ImprovementSignal> signals)
    {
        foreach (var delta in comparison.SecurityChanges)
        {
            if (!string.Equals(delta.BaseStatus, delta.TargetStatus, StringComparison.OrdinalIgnoreCase))
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = "SecurityRegression",
                    Category = "Security",
                    Title = $"Security posture changed: {delta.ControlName}",
                    Description = $"{delta.BaseStatus ?? "—"} → {delta.TargetStatus ?? "—"}",
                    Severity = "High"
                });
            }
        }

        foreach (var delta in comparison.CostChanges)
        {
            if (delta is { BaseCost: not null, TargetCost: not null } && delta.TargetCost > delta.BaseCost)
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = "CostIncrease",
                    Category = "Cost",
                    Title = "Estimated cost increased",
                    Description = $"{delta.BaseCost:0.00} → {delta.TargetCost:0.00}",
                    Severity = "Medium"
                });
            }
        }

        foreach (var d in comparison.DecisionChanges)
        {
            if (string.Equals(d.ChangeType, "Removed", StringComparison.OrdinalIgnoreCase))
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = "DecisionRemoved",
                    Category = "Requirement",
                    Title = $"Decision removed: {d.DecisionKey}",
                    Description = $"Previously: {d.BaseValue ?? "—"}",
                    Severity = "Medium"
                });
            }
        }
    }
}

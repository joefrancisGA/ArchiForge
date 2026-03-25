using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Advisory.Analysis;

/// <summary>
/// Default <see cref="IImprovementSignalAnalyzer"/> implementation driven by manifest gaps and <see cref="ComparisonResult"/> deltas.
/// </summary>
public sealed class ImprovementSignalAnalyzer : IImprovementSignalAnalyzer
{
    private const string CategoryRequirement = "Requirement";
    private const string CategorySecurity = "Security";
    private const string CategoryCompliance = "Compliance";
    private const string CategoryTopology = "Topology";
    private const string CategoryCost = "Cost";
    private const string CategoryRisk = "Risk";

    private const string SignalTypeUncoveredRequirement = "UncoveredRequirement";
    private const string SignalTypeSecurityGap = "SecurityGap";
    private const string SignalTypeComplianceGap = "ComplianceGap";
    private const string SignalTypeTopologyGap = "TopologyGap";
    private const string SignalTypeCostRisk = "CostRisk";
    private const string SignalTypeUnresolvedIssue = "UnresolvedIssue";
    private const string SignalTypeSecurityRegression = "SecurityRegression";
    private const string SignalTypeCostIncrease = "CostIncrease";
    private const string SignalTypeDecisionRemoved = "DecisionRemoved";

    private const string SeverityHigh = "High";
    private const string SeverityMedium = "Medium";
    private const string SeverityCritical = "Critical";

    private const string ChangeTypeRemoved = "Removed";
    /// <inheritdoc />
    /// <remarks>
    /// Currently does not read individual findings from <paramref name="findingsSnapshot"/>; extension points use manifest and comparison only.
    /// </remarks>
    public IReadOnlyList<ImprovementSignal> Analyze(
        GoldenManifest manifest,
        FindingsSnapshot findingsSnapshot,
        ComparisonResult? comparison = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(findingsSnapshot);

        List<ImprovementSignal> signals = [];

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
        foreach (RequirementCoverageItem uncovered in manifest.Requirements.Uncovered)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeUncoveredRequirement,
                Category = CategoryRequirement,
                Title = $"Requirement not covered: {uncovered.RequirementName}",
                Description = string.IsNullOrWhiteSpace(uncovered.RequirementText)
                    ? uncovered.RequirementName
                    : uncovered.RequirementText,
                Severity = SeverityHigh,
                FindingIds = uncovered.SupportingFindingIds.ToList()
            });
        }
    }

    private static void AnalyzeSecuritySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Security.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeSecurityGap,
                Category = CategorySecurity,
                Title = "Security protection gap",
                Description = gap,
                Severity = SeverityHigh
            });
        }
    }

    private static void AnalyzeComplianceSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Compliance.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeComplianceGap,
                Category = CategoryCompliance,
                Title = "Compliance gap detected",
                Description = gap,
                Severity = SeverityHigh
            });
        }
    }

    private static void AnalyzeTopologySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Topology.Gaps)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeTopologyGap,
                Category = CategoryTopology,
                Title = "Topology coverage gap",
                Description = gap,
                Severity = SeverityMedium
            });
        }
    }

    private static void AnalyzeCostSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string risk in manifest.Cost.CostRisks)
        {
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeCostRisk,
                Category = CategoryCost,
                Title = "Cost risk detected",
                Description = risk,
                Severity = SeverityMedium
            });
        }
    }

    private static void AnalyzeUnresolvedIssueSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (ManifestIssue issue in manifest.UnresolvedIssues.Items)
        {
            string sev = string.IsNullOrWhiteSpace(issue.Severity) ? SeverityMedium : issue.Severity;
            signals.Add(new ImprovementSignal
            {
                SignalType = SignalTypeUnresolvedIssue,
                Category = CategoryRisk,
                Title = issue.Title,
                Description = issue.Description,
                Severity = string.Equals(sev, SeverityCritical, StringComparison.OrdinalIgnoreCase) ? SeverityCritical : sev,
                FindingIds = issue.SupportingFindingIds.ToList()
            });
        }
    }

    private static void AnalyzeComparisonSignals(ComparisonResult comparison, List<ImprovementSignal> signals)
    {
        foreach (SecurityDelta delta in comparison.SecurityChanges)
        {
            if (!string.Equals(delta.BaseStatus, delta.TargetStatus, StringComparison.OrdinalIgnoreCase))
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = SignalTypeSecurityRegression,
                    Category = CategorySecurity,
                    Title = $"Security posture changed: {delta.ControlName}",
                    Description = $"{delta.BaseStatus ?? "—"} → {delta.TargetStatus ?? "—"}",
                    Severity = SeverityHigh
                });
            }
        }

        foreach (CostDelta delta in comparison.CostChanges)
        {
            if (delta is { BaseCost: not null, TargetCost: not null } && delta.TargetCost > delta.BaseCost)
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = SignalTypeCostIncrease,
                    Category = CategoryCost,
                    Title = "Estimated cost increased",
                    Description = $"{delta.BaseCost:0.00} → {delta.TargetCost:0.00}",
                    Severity = SeverityMedium
                });
            }
        }

        foreach (DecisionDelta d in comparison.DecisionChanges)
        {
            if (string.Equals(d.ChangeType, ChangeTypeRemoved, StringComparison.OrdinalIgnoreCase))
            {
                signals.Add(new ImprovementSignal
                {
                    SignalType = SignalTypeDecisionRemoved,
                    Category = CategoryRequirement,
                    Title = $"Decision removed: {d.DecisionKey}",
                    Description = $"Previously: {d.BaseValue ?? "—"}",
                    Severity = SeverityMedium
                });
            }
        }
    }
}

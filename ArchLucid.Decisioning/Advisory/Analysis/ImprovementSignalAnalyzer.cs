using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Advisory.Analysis;

/// <summary>
///     Default <see cref="IImprovementSignalAnalyzer" /> implementation driven by manifest gaps and
///     <see cref="ComparisonResult" /> deltas.
/// </summary>
public sealed class ImprovementSignalAnalyzer : IImprovementSignalAnalyzer
{
    private const string ChangeTypeRemoved = "Removed";

    /// <inheritdoc />
    /// <remarks>
    ///     Currently does not read individual findings from <paramref name="findingsSnapshot" />; extension points use
    ///     manifest and comparison only.
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
        AnalyzePolicyViolationSignals(manifest, signals);
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

            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.UncoveredRequirement,
                Category = ImprovementSignalCategories.Requirement,
                Title = $"Requirement not covered: {uncovered.RequirementName}",
                Description = string.IsNullOrWhiteSpace(uncovered.RequirementText)
                    ? uncovered.RequirementName
                    : uncovered.RequirementText,
                Severity = ImprovementSignalSeverities.High,
                FindingIds = uncovered.SupportingFindingIds.ToList()
            });
    }

    private static void AnalyzeSecuritySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Security.Gaps)

            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.SecurityGap,
                Category = ImprovementSignalCategories.Security,
                Title = "Security protection gap",
                Description = gap,
                Severity = ImprovementSignalSeverities.High
            });
    }

    private static void AnalyzeComplianceSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Compliance.Gaps)

            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.ComplianceGap,
                Category = ImprovementSignalCategories.Compliance,
                Title = "Compliance gap detected",
                Description = gap,
                Severity = ImprovementSignalSeverities.High
            });
    }

    private static void AnalyzePolicyViolationSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        signals.AddRange(manifest.Policy.Violations.Select(violation => new ImprovementSignal
        {
            SignalType = ImprovementSignalTypes.PolicyViolation,
            Category = ImprovementSignalCategories.Compliance,
            Title = string.IsNullOrWhiteSpace(violation.ControlName)
                ? "Policy violation"
                : $"Policy violation: {violation.ControlName}",
            Description = string.IsNullOrWhiteSpace(violation.Description)
                ? violation.ControlId
                : violation.Description,
            Severity = ImprovementSignalSeverities.High
        }));
    }

    private static void AnalyzeTopologySignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string gap in manifest.Topology.Gaps)

            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.TopologyGap,
                Category = ImprovementSignalCategories.Topology,
                Title = "Topology coverage gap",
                Description = gap,
                Severity = ImprovementSignalSeverities.Medium
            });
    }

    private static void AnalyzeCostSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (string risk in manifest.Cost.CostRisks)

            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.CostRisk,
                Category = ImprovementSignalCategories.Cost,
                Title = "Cost risk detected",
                Description = risk,
                Severity = ImprovementSignalSeverities.Medium
            });
    }

    private static void AnalyzeUnresolvedIssueSignals(GoldenManifest manifest, List<ImprovementSignal> signals)
    {
        foreach (ManifestIssue issue in manifest.UnresolvedIssues.Items)
        {
            string sev = string.IsNullOrWhiteSpace(issue.Severity)
                ? ImprovementSignalSeverities.Medium
                : issue.Severity;
            signals.Add(new ImprovementSignal
            {
                SignalType = ImprovementSignalTypes.UnresolvedIssue,
                Category = ImprovementSignalCategories.Risk,
                Title = issue.Title,
                Description = issue.Description,
                Severity = string.Equals(sev, ImprovementSignalSeverities.Critical, StringComparison.OrdinalIgnoreCase)
                    ? ImprovementSignalSeverities.Critical
                    : sev,
                FindingIds = issue.SupportingFindingIds.ToList()
            });
        }
    }

    private static void AnalyzeComparisonSignals(ComparisonResult comparison, List<ImprovementSignal> signals)
    {
        foreach (SecurityDelta delta in comparison.SecurityChanges)

            if (!string.Equals(delta.BaseStatus, delta.TargetStatus, StringComparison.OrdinalIgnoreCase))

                signals.Add(new ImprovementSignal
                {
                    SignalType = ImprovementSignalTypes.SecurityRegression,
                    Category = ImprovementSignalCategories.Security,
                    Title = $"Security posture changed: {delta.ControlName}",
                    Description = $"{delta.BaseStatus ?? "—"} → {delta.TargetStatus ?? "—"}",
                    Severity = ImprovementSignalSeverities.High
                });


        foreach (CostDelta delta in comparison.CostChanges)

            if (delta is { BaseCost: not null, TargetCost: not null } && delta.TargetCost > delta.BaseCost)

                signals.Add(new ImprovementSignal
                {
                    SignalType = ImprovementSignalTypes.CostIncrease,
                    Category = ImprovementSignalCategories.Cost,
                    Title = "Estimated cost increased",
                    Description = $"{delta.BaseCost:0.00} → {delta.TargetCost:0.00}",
                    Severity = ImprovementSignalSeverities.Medium
                });


        foreach (DecisionDelta d in comparison.DecisionChanges)

            if (string.Equals(d.ChangeType, ChangeTypeRemoved, StringComparison.OrdinalIgnoreCase))

                signals.Add(new ImprovementSignal
                {
                    SignalType = ImprovementSignalTypes.DecisionRemoved,
                    Category = ImprovementSignalCategories.Requirement,
                    Title = $"Decision removed: {d.DecisionKey}",
                    Description = $"Previously: {d.BaseValue ?? "—"}",
                    Severity = ImprovementSignalSeverities.Medium
                });
    }
}

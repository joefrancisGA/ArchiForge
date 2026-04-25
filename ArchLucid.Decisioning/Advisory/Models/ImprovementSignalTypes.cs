namespace ArchLucid.Decisioning.Advisory.Models;

/// <summary>
///     Canonical <see cref="ImprovementSignal.SignalType" /> values emitted by
///     <see cref="Analysis.ImprovementSignalAnalyzer" />
///     and interpreted by <see cref="Services.RecommendationGenerator" />.
/// </summary>
public static class ImprovementSignalTypes
{
    public const string UncoveredRequirement = "UncoveredRequirement";
    public const string SecurityGap = "SecurityGap";
    public const string ComplianceGap = "ComplianceGap";
    public const string TopologyGap = "TopologyGap";
    public const string CostRisk = "CostRisk";
    public const string UnresolvedIssue = "UnresolvedIssue";
    public const string SecurityRegression = "SecurityRegression";
    public const string CostIncrease = "CostIncrease";
    public const string DecisionRemoved = "DecisionRemoved";

    /// <summary>Entries from <c>GoldenManifest.Policy.Violations</c> surfaced for advisory scoring.</summary>
    public const string PolicyViolation = "PolicyViolation";
}

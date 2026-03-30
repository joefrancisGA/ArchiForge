namespace ArchiForge.Core.Comparison;

/// <summary>
/// Structured delta between two golden manifests: run ids, per-domain change lists, and short human-readable summary lines.
/// </summary>
/// <remarks>
/// Produced by <c>ArchiForge.Decisioning.Comparison.IComparisonService.Compare</c>. Consumed by HTTP <c>GET api/compare</c>, advisory pipelines, and <c>IImprovementSignalAnalyzer</c> when a comparison is available.
/// </remarks>
public class ComparisonResult
{
    /// <summary>Run id copied from the base manifest.</summary>
    public Guid BaseRunId { get; set; }

    /// <summary>Run id copied from the target manifest.</summary>
    public Guid TargetRunId { get; set; }

    /// <summary>Decision add/remove/option changes.</summary>
    public List<DecisionDelta> DecisionChanges { get; set; } = [];

    /// <summary>Coverage bucket or metadata changes by requirement name.</summary>
    public List<RequirementDelta> RequirementChanges { get; set; } = [];

    /// <summary>Control-level status transitions (including add/remove as null base/target).</summary>
    public List<SecurityDelta> SecurityChanges { get; set; } = [];

    /// <summary>Resource ids added or removed between topology sections.</summary>
    public List<TopologyDelta> TopologyChanges { get; set; } = [];

    /// <summary>Populated when <c>MaxMonthlyCost</c> differs between manifests (at most one entry today).</summary>
    public List<CostDelta> CostChanges { get; set; } = [];

    /// <summary>High-level counts/messages built by <c>ArchiForge.Decisioning.Comparison.ComparisonService</c>.</summary>
    public List<string> SummaryHighlights { get; set; } = [];

    /// <summary>
    /// Sum of decision, requirement, security, topology, and cost delta row counts (set by <c>ComparisonService</c> for operator UIs).
    /// </summary>
    public int TotalDeltaCount { get; set; }
}

/// <summary>One decision key that was added, removed, or had a different selected option.</summary>
public class DecisionDelta
{
    /// <summary>Stable key (decision id or category/title composite).</summary>
    public string DecisionKey { get; set; } = null!;

    /// <summary>Selected option on the base manifest when applicable.</summary>
    public string? BaseValue { get; set; }

    /// <summary>Selected option on the target manifest when applicable.</summary>
    public string? TargetValue { get; set; }

    /// <summary><c>Added</c>, <c>Removed</c>, or <c>Modified</c>.</summary>
    public string ChangeType { get; set; } = null!;
}

/// <summary>Requirement name with a coverage-related change.</summary>
public class RequirementDelta
{
    /// <summary>Requirement identifier from the manifest section.</summary>
    public string RequirementName { get; set; } = null!;

    /// <summary><c>Covered</c>, <c>Uncovered</c>, <c>Removed</c>, or <c>Changed</c> (per comparer logic).</summary>
    public string ChangeType { get; set; } = null!;
}

/// <summary>Security control status change or appearance/disappearance.</summary>
/// <remarks>When a control exists only on one side, the opposite status property may be <see langword="null"/>.</remarks>
public class SecurityDelta
{
    /// <summary>Human-readable control name (aligned with manifest item).</summary>
    public string ControlName { get; set; } = null!;

    /// <summary>Status on the base manifest.</summary>
    public string? BaseStatus { get; set; }

    /// <summary>Status on the target manifest.</summary>
    public string? TargetStatus { get; set; }
}

/// <summary>Topology resource added or removed between manifests.</summary>
public class TopologyDelta
{
    /// <summary>Resource identifier from topology lists.</summary>
    public string Resource { get; set; } = null!;

    /// <summary><c>Added</c> or <c>Removed</c>.</summary>
    public string ChangeType { get; set; } = null!;
}

/// <summary>Change to estimated maximum monthly cost when both sides expose a value.</summary>
public class CostDelta
{
    /// <summary>Base manifest <c>MaxMonthlyCost</c>.</summary>
    public decimal? BaseCost { get; set; }

    /// <summary>Target manifest <c>MaxMonthlyCost</c>.</summary>
    public decimal? TargetCost { get; set; }
}

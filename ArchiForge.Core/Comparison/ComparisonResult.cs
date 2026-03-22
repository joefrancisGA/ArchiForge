namespace ArchiForge.Core.Comparison;

public class ComparisonResult
{
    public Guid BaseRunId { get; set; }
    public Guid TargetRunId { get; set; }

    public List<DecisionDelta> DecisionChanges { get; set; } = [];
    public List<RequirementDelta> RequirementChanges { get; set; } = [];
    public List<SecurityDelta> SecurityChanges { get; set; } = [];
    public List<TopologyDelta> TopologyChanges { get; set; } = [];
    public List<CostDelta> CostChanges { get; set; } = [];

    public List<string> SummaryHighlights { get; set; } = [];
}

public class DecisionDelta
{
    public string DecisionKey { get; set; } = null!;

    public string? BaseValue { get; set; }
    public string? TargetValue { get; set; }

    /// <summary>Added, Removed, or Modified.</summary>
    public string ChangeType { get; set; } = null!;
}

public class RequirementDelta
{
    public string RequirementName { get; set; } = null!;

    /// <summary>Covered, Uncovered, or Changed.</summary>
    public string ChangeType { get; set; } = null!;
}

public class SecurityDelta
{
    public string ControlName { get; set; } = null!;

    public string? BaseStatus { get; set; }
    public string? TargetStatus { get; set; }
}

public class TopologyDelta
{
    public string Resource { get; set; } = null!;

    /// <summary>Added or Removed.</summary>
    public string ChangeType { get; set; } = null!;
}

public class CostDelta
{
    public decimal? BaseCost { get; set; }
    public decimal? TargetCost { get; set; }
}

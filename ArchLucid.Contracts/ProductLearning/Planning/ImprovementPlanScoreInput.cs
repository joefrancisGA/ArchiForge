namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Metrics for one plan in a prioritization batch. Map from 58R rollups / pilot signals (caller supplies counts).
/// </summary>
public sealed class ImprovementPlanScoreInput
{
    public ImprovementPlan Plan { get; init; } = null!;

    /// <summary>Total pilot signals backing the theme (frequency).</summary>
    public int EvidenceSignalCount { get; init; }

    /// <summary>Rejected disposition count in the theme window (severity).</summary>
    public int RejectedCount { get; init; }

    /// <summary>Revised disposition count (maps to “needs revision”).</summary>
    public int RevisedCount { get; init; }

    /// <summary>Needs-follow-up disposition count (severity).</summary>
    public int NeedsFollowUpCount { get; init; }

    /// <summary>Optional mean trust in [0,1]; null uses neutral trust stress.</summary>
    public double? AverageTrustScore { get; init; }

    /// <summary>Distinct artifact facets (breadth).</summary>
    public int AffectedArtifactTypeCount { get; init; }
}

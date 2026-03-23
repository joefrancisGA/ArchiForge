namespace ArchiForge.Decisioning.Advisory.Models;

/// <summary>
/// One prioritized advisory item in an <see cref="ImprovementPlan"/>, suitable for UI lists and digest sections.
/// </summary>
public class ImprovementRecommendation
{
    /// <summary>Stable id for idempotent workflows (e.g. recommendation persistence).</summary>
    public Guid RecommendationId { get; set; } = Guid.NewGuid();

    /// <summary>Short headline.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Domain grouping (Security, Cost, etc.).</summary>
    public string Category { get; set; } = null!;

    /// <summary>Why this recommendation exists.</summary>
    public string Rationale { get; set; } = null!;

    /// <summary>Concrete next step for operators.</summary>
    public string SuggestedAction { get; set; } = null!;

    /// <summary>Relative urgency label (e.g. Low/Medium/High).</summary>
    public string Urgency { get; set; } = "Medium";

    /// <summary>Expected benefit if addressed.</summary>
    public string ExpectedImpact { get; set; } = null!;

    /// <summary>Backing finding ids (from golden/findings).</summary>
    public List<string> SupportingFindingIds { get; set; } = [];

    /// <summary>Backing decision keys.</summary>
    public List<string> SupportingDecisionIds { get; set; } = [];

    /// <summary>Optional artifact references for drill-down.</summary>
    public List<string> SupportingArtifactIds { get; set; } = [];

    /// <summary>Higher sorts earlier; produced by the advisor from signals and heuristics.</summary>
    public int PriorityScore
    {
        get; set;
    }
}

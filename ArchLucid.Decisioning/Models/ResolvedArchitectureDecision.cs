namespace ArchiForge.Decisioning.Models;

public class ResolvedArchitectureDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string SelectedOption { get; set; } = null!;
    public string Rationale { get; set; } = null!;
    public List<string> SupportingFindingIds { get; set; } = [];

    /// <summary>Optional graph node identifiers tied to this decision (phase-1 relational extraction).</summary>
    public List<string> RelatedNodeIds { get; set; } = [];

    /// <summary>Optional opaque JSON sidecar for decision payloads not modeled in columns.</summary>
    public string? RawDecisionJson { get; set; }
}


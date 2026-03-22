namespace ArchiForge.Decisioning.Advisory.Models;

public class ImprovementRecommendation
{
    public Guid RecommendationId { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = null!;
    public string Category { get; set; } = null!;

    public string Rationale { get; set; } = null!;
    public string SuggestedAction { get; set; } = null!;

    public string Urgency { get; set; } = "Medium";
    public string ExpectedImpact { get; set; } = null!;

    public List<string> SupportingFindingIds { get; set; } = [];
    public List<string> SupportingDecisionIds { get; set; } = [];
    public List<string> SupportingArtifactIds { get; set; } = [];

    public int PriorityScore { get; set; }
}

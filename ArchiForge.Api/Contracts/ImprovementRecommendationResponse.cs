namespace ArchiForge.Api.Contracts;

public class ImprovementRecommendationResponse
{
    public Guid RecommendationId { get; set; }
    public string Title { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Rationale { get; set; } = null!;
    public string SuggestedAction { get; set; } = null!;
    public string Urgency { get; set; } = null!;
    public string ExpectedImpact { get; set; } = null!;
    public int PriorityScore { get; set; }
}

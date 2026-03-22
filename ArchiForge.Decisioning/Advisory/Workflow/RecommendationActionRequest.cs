namespace ArchiForge.Decisioning.Advisory.Workflow;

public class RecommendationActionRequest
{
    public string Action { get; set; } = default!;
    public string? Comment { get; set; }
    public string? Rationale { get; set; }
}

namespace ArchiForge.Decisioning.Models;

public class ResolvedArchitectureDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string SelectedOption { get; set; } = null!;
    public string Rationale { get; set; } = null!;
    public List<string> SupportingFindingIds { get; set; } = [];
}


namespace ArchiForge.Decisioning.Models;

public class ResolvedArchitectureDecision
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Category { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string SelectedOption { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public List<string> SupportingFindingIds { get; set; } = [];
}


namespace ArchiForge.Decisioning.Advisory.Models;

public class ImprovementSignal
{
    public string SignalType { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Severity { get; set; } = "Medium";

    public List<string> FindingIds { get; set; } = [];
    public List<string> DecisionIds { get; set; } = [];
}

namespace ArchiForge.Decisioning.Models;

public class ExplainabilityTrace
{
    public List<string> GraphNodeIdsExamined { get; set; } = [];
    public List<string> RulesApplied { get; set; } = [];
    public List<string> DecisionsTaken { get; set; } = [];
    public List<string> AlternativePathsConsidered { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}


namespace ArchiForge.Decisioning.Models;

public class DecisionRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsMandatory { get; set; }

    public string AppliesToFindingType { get; set; } = null!;
    public string Action { get; set; } = null!;
    // allow | require | reject | prefer

    public Dictionary<string, string> Criteria { get; set; } = new();
}


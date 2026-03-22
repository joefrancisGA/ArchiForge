namespace ArchiForge.Core.Explanation;

public class ExplanationResult
{
    public string Summary { get; set; } = string.Empty;

    public List<string> KeyDrivers { get; set; } = [];

    public List<string> RiskImplications { get; set; } = [];

    public List<string> CostImplications { get; set; } = [];

    public List<string> ComplianceImplications { get; set; } = [];

    public string DetailedNarrative { get; set; } = string.Empty;
}

public class ComparisonExplanationResult
{
    public string HighLevelSummary { get; set; } = string.Empty;

    public List<string> MajorChanges { get; set; } = [];

    public List<string> KeyTradeoffs { get; set; } = [];

    public string Narrative { get; set; } = string.Empty;
}

namespace ArchiForge.Contracts.Manifest;

public sealed class ManifestGovernance
{
    public List<string> ComplianceTags { get; set; } = [];

    public List<string> PolicyConstraints { get; set; } = [];

    public List<string> RequiredControls { get; set; } = [];

    public string RiskClassification { get; set; } = "Moderate";

    public string CostClassification { get; set; } = "Moderate";
}
namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceRuleDocument
{
    public string RuleId { get; set; } = null!;

    public string ControlId { get; set; } = null!;

    public string ControlName { get; set; } = null!;

    public string AppliesToCategory { get; set; } = null!;

    public string RequiredNodeType { get; set; } = null!;

    public string RequiredEdgeType { get; set; } = null!;

    public string Severity { get; set; } = "Warning";

    public string Description { get; set; } = null!;
}

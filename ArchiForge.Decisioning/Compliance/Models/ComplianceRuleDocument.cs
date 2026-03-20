namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceRuleDocument
{
    public string RuleId { get; set; } = default!;

    public string ControlId { get; set; } = default!;

    public string ControlName { get; set; } = default!;

    public string AppliesToCategory { get; set; } = default!;

    public string RequiredNodeType { get; set; } = default!;

    public string RequiredEdgeType { get; set; } = default!;

    public string Severity { get; set; } = "Warning";

    public string Description { get; set; } = default!;
}

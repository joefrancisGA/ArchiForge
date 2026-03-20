namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceViolation
{
    public string RuleId { get; set; } = default!;

    public string ControlId { get; set; } = default!;

    public string ControlName { get; set; } = default!;

    public string AppliesToCategory { get; set; } = default!;

    public string Severity { get; set; } = default!;

    public string Description { get; set; } = default!;

    public List<string> AffectedNodeIds { get; set; } = [];

    public List<string> AffectedResources { get; set; } = [];
}

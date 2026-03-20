namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceRulePackDocument
{
    public string RulePackId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Version { get; set; } = default!;

    public List<ComplianceRuleDocument> Rules { get; set; } = [];
}

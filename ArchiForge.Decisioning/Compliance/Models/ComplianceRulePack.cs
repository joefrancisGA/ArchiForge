namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceRulePack
{
    public string RulePackId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Version { get; set; } = default!;

    public string RulePackHash { get; set; } = default!;

    public string SourcePath { get; set; } = default!;

    public List<ComplianceRule> Rules { get; set; } = [];
}

namespace ArchiForge.Decisioning.Compliance.Models;

public class ComplianceRulePack
{
    public string RulePackId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string RulePackHash { get; set; } = null!;

    public string SourcePath { get; set; } = null!;

    public List<ComplianceRule> Rules { get; set; } = [];
}

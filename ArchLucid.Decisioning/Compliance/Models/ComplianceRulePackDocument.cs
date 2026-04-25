namespace ArchLucid.Decisioning.Compliance.Models;

public class ComplianceRulePackDocument
{
    public string RulePackId
    {
        get;
        set;
    } = null!;

    public string Name
    {
        get;
        set;
    } = null!;

    public string Version
    {
        get;
        set;
    } = null!;

    public List<ComplianceRuleDocument> Rules
    {
        get;
        set;
    } = [];
}

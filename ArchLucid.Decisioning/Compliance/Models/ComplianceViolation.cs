namespace ArchLucid.Decisioning.Compliance.Models;

public class ComplianceViolation
{
    public string RuleId
    {
        get;
        set;
    } = null!;

    public string ControlId
    {
        get;
        set;
    } = null!;

    public string ControlName
    {
        get;
        set;
    } = null!;

    public string AppliesToCategory
    {
        get;
        set;
    } = null!;

    public string Severity
    {
        get;
        set;
    } = null!;

    public string Description
    {
        get;
        set;
    } = null!;

    public List<string> AffectedNodeIds
    {
        get;
        set;
    } = [];

    public List<string> AffectedResources
    {
        get;
        set;
    } = [];
}

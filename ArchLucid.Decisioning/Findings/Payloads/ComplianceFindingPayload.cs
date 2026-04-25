namespace ArchLucid.Decisioning.Findings.Payloads;

public class ComplianceFindingPayload
{
    public string RulePackId
    {
        get;
        set;
    } = null!;

    public string RulePackVersion
    {
        get;
        set;
    } = null!;

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

    public List<string> AffectedResources
    {
        get;
        set;
    } = [];
}

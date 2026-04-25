namespace ArchLucid.Decisioning.Findings.Payloads;

public class SecurityControlFindingPayload
{
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

    public string Status
    {
        get;
        set;
    } = null!;

    public string Impact
    {
        get;
        set;
    } = null!;
}

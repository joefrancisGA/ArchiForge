namespace ArchLucid.Contracts.Governance;

/// <summary>Single severity tally for a dry-run run item (e.g. <c>{ Severity = "Critical", Count = 2 }</c>).</summary>
public sealed class PolicyPackDryRunSeverityCount
{
    public string Severity
    {
        get; init;
    } = string.Empty;

    public int Count
    {
        get; init;
    }
}

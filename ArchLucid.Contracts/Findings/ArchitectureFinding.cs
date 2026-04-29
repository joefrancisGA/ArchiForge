using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Findings;

public sealed class ArchitectureFinding
{
    public string FindingId
    {
        get;
        set;
    } = Guid.NewGuid().ToString("N");

    public AgentType SourceAgent
    {
        get;
        set;
    }

    public FindingSeverity Severity
    {
        get;
        set;
    } = FindingSeverity.Info;

    /// <summary>Optional self-rated confidence from the producing agent when mapped from <c>AgentResult</c>.</summary>
    public double? ConfidenceScore
    {
        get;
        set;
    }

    public string Category
    {
        get;
        set;
    } = string.Empty;

    public string Message
    {
        get;
        set;
    } = string.Empty;

    public List<string> EvidenceRefs
    {
        get;
        set;
    } = [];
}

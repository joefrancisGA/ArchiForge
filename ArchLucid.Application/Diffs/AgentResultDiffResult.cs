namespace ArchLucid.Application.Diffs;

public sealed class AgentResultDiffResult
{
    public string LeftRunId
    {
        get;
        set;
    } = string.Empty;

    public string RightRunId
    {
        get;
        set;
    } = string.Empty;

    public List<AgentResultDelta> AgentDeltas
    {
        get;
        set;
    } = [];

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}

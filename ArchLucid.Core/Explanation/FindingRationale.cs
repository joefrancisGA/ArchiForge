namespace ArchLucid.Core.Explanation;

/// <summary>Unified finding rationale for authority pipeline findings or coordinator agent findings.</summary>
public sealed class FindingRationale
{
    public string FindingId
    {
        get;
        set;
    } = string.Empty;

    public string Title
    {
        get;
        set;
    } = string.Empty;

    public string Severity
    {
        get;
        set;
    } = string.Empty;

    public string Rationale
    {
        get;
        set;
    } = string.Empty;

    public string Category
    {
        get;
        set;
    } = string.Empty;

    public string EngineType
    {
        get;
        set;
    } = string.Empty;

    public List<string> RelatedNodeIds
    {
        get;
        set;
    } = [];

    public List<string> RecommendedActions
    {
        get;
        set;
    } = [];

    public FindingTraceCompletenessScore? TraceCompleteness
    {
        get;
        set;
    }
}

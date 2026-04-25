namespace ArchLucid.Decisioning.Models;

public class Finding
{
    /// <summary>Schema version of this finding record (increment when envelope or payload contracts change).</summary>
    public int FindingSchemaVersion
    {
        get;
        set;
    } = FindingsSchema.CurrentFindingVersion;

    public string FindingId
    {
        get;
        set;
    } = Guid.NewGuid().ToString("N");

    public string FindingType
    {
        get;
        set;
    } = null!;

    public string Category
    {
        get;
        set;
    } = null!;

    public string EngineType
    {
        get;
        set;
    } = null!;

    public FindingSeverity Severity
    {
        get;
        set;
    }

    public string Title
    {
        get;
        set;
    } = null!;

    public string Rationale
    {
        get;
        set;
    } = null!;

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

    public Dictionary<string, string> Properties
    {
        get;
        set;
    } = new();

    public object? Payload
    {
        get;
        set;
    }

    public string? PayloadType
    {
        get;
        set;
    }

    public ExplainabilityTrace Trace
    {
        get;
        set;
    } = new();
}

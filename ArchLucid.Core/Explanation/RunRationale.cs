namespace ArchLucid.Core.Explanation;

/// <summary>Cross-pipeline rationale envelope for operator and UI consumption.</summary>
public sealed class RunRationale
{
    public Guid RunId
    {
        get;
        set;
    }

    /// <summary>
    ///     <c>authority</c> when an authority findings snapshot exists; <c>coordinator</c> when only the architecture run
    ///     aggregate applies.
    /// </summary>
    public string PipelineType
    {
        get;
        set;
    } = string.Empty;

    public string Summary
    {
        get;
        set;
    } = string.Empty;

    public List<FindingRationale> Findings
    {
        get;
        set;
    } = [];

    public List<DecisionTraceEntry> DecisionTraceEntries
    {
        get;
        set;
    } = [];

    public bool ProvenanceAvailable
    {
        get;
        set;
    }

    public bool ExplanationAvailable
    {
        get;
        set;
    }
}

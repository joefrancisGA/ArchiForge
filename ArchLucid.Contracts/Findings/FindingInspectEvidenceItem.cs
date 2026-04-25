namespace ArchLucid.Contracts.Findings;

/// <summary>
/// One persisted citation row for a finding inspector (graph node id, optional artifact coordinate, optional excerpt).
/// </summary>
public sealed class FindingInspectEvidenceItem
{
    /// <summary>Optional golden-manifest artifact id when the citation resolves to a synthesized bundle row.</summary>
    public string? ArtifactId
    {
        get;
        init;
    }

    /// <summary>Optional line or byte span label (engine-defined; often null for graph-only citations).</summary>
    public string? LineRange
    {
        get;
        init;
    }

    /// <summary>Short human-readable excerpt or the raw citation token when no excerpt exists.</summary>
    public string? Excerpt
    {
        get;
        init;
    }
}

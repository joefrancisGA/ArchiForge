namespace ArchLucid.KnowledgeGraph.Configuration;

/// <summary>In-process cache options for hydrated <see cref="Models.GraphSnapshot" /> projection reads keyed by authority scope + run + graph ids.</summary>
public sealed class KnowledgeGraphProjectionCacheOptions
{
    public const string SectionName = "ArchLucid:KnowledgeGraph:ProjectionCache";

    /// <summary>Disable read-through caching entirely (queries always hit persistent stores).</summary>
    public bool Enabled
    {
        get;
        set;
    } = true;

    /// <summary>TTL applied to projection entries (<c>null</c> results are never cached).</summary>
    public int AbsoluteExpirationSeconds
    {
        get;
        set;
    } = 300;
}

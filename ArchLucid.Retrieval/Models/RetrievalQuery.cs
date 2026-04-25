namespace ArchLucid.Retrieval.Models;

/// <summary>
///     Scoped semantic search request: embedding is computed from <see cref="QueryText" /> by
///     <see cref="ArchLucid.Retrieval.Queries.RetrievalQueryService" />.
/// </summary>
public class RetrievalQuery
{
    /// <summary>Tenant filter (required).</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Workspace filter (required).</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Project filter (required).</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>
    ///     Optional run facet; behavior depends on <see cref="ArchLucid.Retrieval.Indexing.IVectorIndex" />
    ///     implementation.
    /// </summary>
    public Guid? RunId
    {
        get;
        set;
    }

    /// <summary>Optional manifest facet.</summary>
    public Guid? ManifestId
    {
        get;
        set;
    }

    /// <summary>Natural-language query to embed.</summary>
    public string QueryText
    {
        get;
        set;
    } = null!;

    /// <summary>Maximum hits to return (callers may clamp, e.g. HTTP search caps at 50).</summary>
    public int TopK
    {
        get;
        set;
    } = 8;
}

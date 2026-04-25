namespace ArchLucid.Retrieval.Models;

/// <summary>
///     Indexed unit stored in <see cref="ArchLucid.Retrieval.Indexing.IVectorIndex" /> (text slice + embedding + scope
///     metadata).
/// </summary>
/// <remarks>
///     Produced by <see cref="ArchLucid.Retrieval.Indexing.RetrievalIndexingService" /> from
///     <see cref="RetrievalDocument" />.
/// </remarks>
public class RetrievalChunk
{
    public string ChunkId
    {
        get;
        set;
    } = null!;

    public string DocumentId
    {
        get;
        set;
    } = null!;

    public Guid TenantId
    {
        get;
        set;
    }

    public Guid WorkspaceId
    {
        get;
        set;
    }

    public Guid ProjectId
    {
        get;
        set;
    }

    public Guid? RunId
    {
        get;
        set;
    }

    public Guid? ManifestId
    {
        get;
        set;
    }

    public string SourceType
    {
        get;
        set;
    } = null!;

    public string SourceId
    {
        get;
        set;
    } = null!;

    public string Title
    {
        get;
        set;
    } = null!;

    /// <summary>Chunk text (substring of document content).</summary>
    public string Text
    {
        get;
        set;
    } = null!;

    /// <summary>Zero-based order within the parent document.</summary>
    public int ChunkOrdinal
    {
        get;
        set;
    }

    /// <summary>Dense embedding aligned with <see cref="Text" />.</summary>
    public float[] Embedding
    {
        get;
        set;
    } = [];

    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;
}

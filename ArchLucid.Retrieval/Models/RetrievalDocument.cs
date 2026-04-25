namespace ArchLucid.Retrieval.Models;

/// <summary>
///     Logical document fed into <see cref="ArchLucid.Retrieval.Indexing.IRetrievalIndexingService" /> before chunking and
///     embedding.
/// </summary>
public class RetrievalDocument
{
    /// <summary>Unique id for idempotent upserts (builder-generated).</summary>
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

    /// <inheritdoc cref="RetrievalHit.SourceType" />
    public string SourceType
    {
        get;
        set;
    } = null!;

    /// <inheritdoc cref="RetrievalHit.SourceId" />
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

    /// <summary>Full text to chunk (JSON or prose depending on builder).</summary>
    public string Content
    {
        get;
        set;
    } = null!;

    /// <summary>Change-detection / dedupe hint (manifest hash, artifact hash, or synthetic).</summary>
    public string ContentHash
    {
        get;
        set;
    } = null!;

    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;
}

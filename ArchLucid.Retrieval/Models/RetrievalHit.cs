namespace ArchiForge.Retrieval.Models;

/// <summary>
/// One ranked chunk returned from <see cref="ArchiForge.Retrieval.Queries.IRetrievalQueryService.SearchAsync"/>.
/// </summary>
public class RetrievalHit
{
    /// <summary>Stable chunk key in the vector index.</summary>
    public string ChunkId { get; set; } = null!;

    /// <summary>Parent logical document id (e.g. manifest or artifact).</summary>
    public string DocumentId { get; set; } = null!;

    /// <summary>High-level source discriminator (Manifest, Artifact, ConversationMessage, etc.).</summary>
    public string SourceType { get; set; } = null!;

    /// <summary>Source-specific id string.</summary>
    public string SourceId { get; set; } = null!;

    /// <summary>Short label for UI or prompt formatting.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Chunk text snippet.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Similarity score (higher is more relevant; exact scale depends on the vector index).</summary>
    public double Score { get; set; }
}

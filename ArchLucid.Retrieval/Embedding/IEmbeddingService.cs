namespace ArchiForge.Retrieval.Embedding;

/// <summary>
/// Text → dense vector embeddings for semantic search and indexing.
/// </summary>
/// <remarks>Used by <see cref="Queries.RetrievalQueryService"/> and <see cref="Indexing.RetrievalIndexingService"/>.</remarks>
public interface IEmbeddingService
{
    /// <summary>Embeds a single string (e.g. user query).</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    /// <summary>Embeds many strings in batch (e.g. chunked document text).</summary>
    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct);
}

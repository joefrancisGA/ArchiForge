using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// Vector store for <see cref="RetrievalChunk"/> upserts and scoped similarity search.
/// </summary>
/// <remarks>Implementation is storage-specific (e.g. in-memory, external vector DB). Not HTTP-aware.</remarks>
public interface IVectorIndex
{
    /// <summary>Inserts or replaces chunked embeddings for the given scope metadata.</summary>
    Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct);

    /// <summary>
    /// Returns up to <see cref="RetrievalQuery.TopK"/> hits filtered by tenant/workspace/project and optional run/manifest facets.
    /// </summary>
    /// <param name="query">Scope filters and result cap.</param>
    /// <param name="queryEmbedding">Embedding of <see cref="RetrievalQuery.QueryText"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct);
}

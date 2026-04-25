using ArchLucid.Retrieval.Models;

namespace ArchLucid.Retrieval.Indexing;

/// <summary>
///     Azure AI Search seam for vector upsert and hybrid/vector query. Wired when <c>Retrieval:VectorIndex</c> uses search
///     (not <see cref="InMemoryVectorIndex" />).
/// </summary>
/// <remarks>Placeholder registration: <see cref="NotConfiguredAzureSearchClient" /> throws with configuration guidance.</remarks>
public interface IAzureSearchClient
{
    /// <summary>Uploads or merges chunk documents with embeddings into the search index.</summary>
    /// <param name="chunks">Chunks to upsert (idempotent by <see cref="RetrievalChunk.ChunkId" />).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct);

    /// <summary>Runs a vector query scoped by <paramref name="query" /> metadata.</summary>
    /// <param name="query">Scope filter and topK configuration.</param>
    /// <param name="queryEmbedding">Pre-computed embedding vector for the user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Ranked retrieval hits, highest similarity first.</returns>
    Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct);
}

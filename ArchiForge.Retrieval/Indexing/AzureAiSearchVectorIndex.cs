using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public sealed class AzureAiSearchVectorIndex(IAzureSearchClient client) : IVectorIndex
{
    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct) =>
        client.UpsertChunksAsync(chunks, ct);

    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct) =>
        client.SearchAsync(query, queryEmbedding, ct);
}

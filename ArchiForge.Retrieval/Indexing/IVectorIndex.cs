using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public interface IVectorIndex
{
    Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct);

    Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct);
}

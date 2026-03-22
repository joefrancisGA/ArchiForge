using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>Production seam for Azure AI Search (vector index). Wire a real implementation when the index is provisioned.</summary>
public interface IAzureSearchClient
{
    Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct);

    Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct);
}

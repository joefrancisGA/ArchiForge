using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>Throws until a real Azure AI Search client is registered.</summary>
public sealed class NotConfiguredAzureSearchClient : IAzureSearchClient
{
    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct) =>
        throw new InvalidOperationException(
            "Azure AI Search is not configured. Register a concrete IAzureSearchClient or use InMemoryVectorIndex (Retrieval:VectorIndex = InMemory).");

    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct) =>
        throw new InvalidOperationException(
            "Azure AI Search is not configured. Register a concrete IAzureSearchClient or use InMemoryVectorIndex (Retrieval:VectorIndex = InMemory).");
}

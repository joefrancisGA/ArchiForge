using System.Diagnostics.CodeAnalysis;

using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// <see cref="IVectorIndex"/> implementation that delegates to <see cref="IAzureSearchClient"/> (Azure AI Search vector index).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Passthrough adapter; all logic lives in IAzureSearchClient which is tested via its interface.")]
public sealed class AzureAiSearchVectorIndex(IAzureSearchClient client) : IVectorIndex
{
    /// <inheritdoc />
    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct) =>
        client.UpsertChunksAsync(chunks, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct) =>
        client.SearchAsync(query, queryEmbedding, ct);
}

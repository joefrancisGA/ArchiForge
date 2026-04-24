using System.Diagnostics.CodeAnalysis;

using ArchLucid.Retrieval.Models;

namespace ArchLucid.Retrieval.Indexing;

/// <summary>
///     Default <see cref="IAzureSearchClient" /> registration when Azure AI Search is not configured; fails fast with an
///     actionable message.
/// </summary>
[ExcludeFromCodeCoverage(Justification =
    "Sentinel class; every method throws InvalidOperationException with no branching logic.")]
public sealed class NotConfiguredAzureSearchClient : IAzureSearchClient
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always — search is not configured.</exception>
    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct)
    {
        throw new InvalidOperationException(
            "Azure AI Search is not configured. Register a concrete IAzureSearchClient or use InMemoryVectorIndex (Retrieval:VectorIndex = InMemory).");
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always — search is not configured.</exception>
    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct)
    {
        throw new InvalidOperationException(
            "Azure AI Search is not configured. Register a concrete IAzureSearchClient or use InMemoryVectorIndex (Retrieval:VectorIndex = InMemory).");
    }
}

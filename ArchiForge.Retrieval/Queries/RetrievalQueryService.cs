using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Queries;

/// <summary>
/// <see cref="IRetrievalQueryService"/> implementation: embed query text, delegate to <see cref="IVectorIndex"/>.
/// </summary>
public sealed class RetrievalQueryService(
    IEmbeddingService embeddingService,
    IVectorIndex vectorIndex) : IRetrievalQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievalHit>> SearchAsync(RetrievalQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.QueryText);

        float[] embedding = await embeddingService.EmbedAsync(query.QueryText, ct).ConfigureAwait(false);
        return await vectorIndex.SearchAsync(query, embedding, ct).ConfigureAwait(false);
    }
}

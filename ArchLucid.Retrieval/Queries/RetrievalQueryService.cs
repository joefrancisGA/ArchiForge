using ArchLucid.Retrieval.Embedding;
using ArchLucid.Retrieval.Indexing;
using ArchLucid.Retrieval.Models;

namespace ArchLucid.Retrieval.Queries;

/// <summary>
///     <see cref="IRetrievalQueryService" /> implementation: embed query text, delegate to <see cref="IVectorIndex" />.
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

        float[] embedding = await embeddingService.EmbedAsync(query.QueryText, ct);
        return await vectorIndex.SearchAsync(query, embedding, ct);
    }
}

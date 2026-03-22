using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Indexing;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Queries;

public sealed class RetrievalQueryService(
    IEmbeddingService embeddingService,
    IVectorIndex vectorIndex) : IRetrievalQueryService
{
    public async Task<IReadOnlyList<RetrievalHit>> SearchAsync(RetrievalQuery query, CancellationToken ct)
    {
        var embedding = await embeddingService.EmbedAsync(query.QueryText, ct).ConfigureAwait(false);
        return await vectorIndex.SearchAsync(query, embedding, ct).ConfigureAwait(false);
    }
}

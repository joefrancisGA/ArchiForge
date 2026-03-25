using ArchiForge.Retrieval.Chunking;
using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

/// <summary>
/// <see cref="IRetrievalIndexingService"/> pipeline: <see cref="ITextChunker"/> → <see cref="IEmbeddingService.EmbedManyAsync"/> → <see cref="RetrievalChunk"/> → <see cref="IVectorIndex.UpsertChunksAsync"/>.
/// </summary>
public sealed class RetrievalIndexingService(
    ITextChunker chunker,
    IEmbeddingService embeddingService,
    IVectorIndex vectorIndex) : IRetrievalIndexingService
{
    /// <inheritdoc />
    public async Task IndexDocumentsAsync(IReadOnlyList<RetrievalDocument> documents, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (documents.Count == 0)
            return;

        List<RetrievalChunk> chunks = [];

        foreach (RetrievalDocument doc in documents)
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<string> split = chunker.Chunk(doc.Content);
            if (split.Count == 0)
                continue;

            IReadOnlyList<float[]> embeddings = await embeddingService.EmbedManyAsync(split, ct).ConfigureAwait(false);
            if (embeddings.Count != split.Count)
                throw new InvalidOperationException("Embedding count must match chunk count.");

            chunks.AddRange(split.Select((t, i) => new RetrievalChunk
            {
                ChunkId = $"{doc.DocumentId}-chunk-{i}",
                DocumentId = doc.DocumentId,
                TenantId = doc.TenantId,
                WorkspaceId = doc.WorkspaceId,
                ProjectId = doc.ProjectId,
                RunId = doc.RunId,
                ManifestId = doc.ManifestId,
                SourceType = doc.SourceType,
                SourceId = doc.SourceId,
                Title = doc.Title,
                Text = t,
                ChunkOrdinal = i,
                Embedding = embeddings[i],
                CreatedUtc = doc.CreatedUtc
            }));
        }

        if (chunks.Count > 0)
            await vectorIndex.UpsertChunksAsync(chunks, ct).ConfigureAwait(false);
    }
}

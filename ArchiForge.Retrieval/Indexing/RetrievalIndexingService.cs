using ArchiForge.Retrieval.Chunking;
using ArchiForge.Retrieval.Embedding;
using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public sealed class RetrievalIndexingService(
    ITextChunker chunker,
    IEmbeddingService embeddingService,
    IVectorIndex vectorIndex) : IRetrievalIndexingService
{
    public async Task IndexDocumentsAsync(IReadOnlyList<RetrievalDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0)
            return;

        var chunks = new List<RetrievalChunk>();

        foreach (var doc in documents)
        {
            var split = chunker.Chunk(doc.Content);
            if (split.Count == 0)
                continue;

            var embeddings = await embeddingService.EmbedManyAsync(split, ct).ConfigureAwait(false);
            if (embeddings.Count != split.Count)
                throw new InvalidOperationException("Embedding count must match chunk count.");

            for (var i = 0; i < split.Count; i++)
            {
                chunks.Add(new RetrievalChunk
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
                    Text = split[i],
                    ChunkOrdinal = i,
                    Embedding = embeddings[i],
                    CreatedUtc = doc.CreatedUtc
                });
            }
        }

        if (chunks.Count > 0)
            await vectorIndex.UpsertChunksAsync(chunks, ct).ConfigureAwait(false);
    }
}

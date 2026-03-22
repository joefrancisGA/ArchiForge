using ArchiForge.Retrieval.Models;

namespace ArchiForge.Retrieval.Indexing;

public sealed class InMemoryVectorIndex : IVectorIndex
{
    private readonly List<RetrievalChunk> _chunks = [];
    private readonly object _sync = new();

    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct)
    {
        lock (_sync)
        {
            foreach (var chunk in chunks)
            {
                _chunks.RemoveAll(x => x.ChunkId == chunk.ChunkId);
                _chunks.Add(chunk);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct)
    {
        lock (_sync)
        {
            var hits = _chunks
                .Where(x =>
                    x.TenantId == query.TenantId &&
                    x.WorkspaceId == query.WorkspaceId &&
                    x.ProjectId == query.ProjectId &&
                    (!query.RunId.HasValue || x.RunId == query.RunId) &&
                    (!query.ManifestId.HasValue || x.ManifestId == query.ManifestId))
                .Select(x => new RetrievalHit
                {
                    ChunkId = x.ChunkId,
                    DocumentId = x.DocumentId,
                    SourceType = x.SourceType,
                    SourceId = x.SourceId,
                    Title = x.Title,
                    Text = x.Text,
                    Score = Cosine(queryEmbedding, x.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(query.TopK)
                .ToList();

            return Task.FromResult<IReadOnlyList<RetrievalHit>>(hits);
        }
    }

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0;

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}

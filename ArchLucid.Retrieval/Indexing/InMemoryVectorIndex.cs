using ArchLucid.Retrieval.Models;

namespace ArchLucid.Retrieval.Indexing;

/// <summary>
///     Thread-safe in-memory <see cref="IVectorIndex" /> using cosine similarity over stored embeddings (dev/tests or
///     single-node deployments).
/// </summary>
/// <remarks>
///     Replaces existing rows by <see cref="RetrievalChunk.ChunkId" /> on upsert. Filters require exact
///     tenant/workspace/project match; optional run/manifest must match when provided.
/// </remarks>
public sealed class InMemoryVectorIndex : IVectorIndex
{
    private const int MaxChunks = 10_000;

    private readonly List<RetrievalChunk> _chunks = [];
    private readonly Lock _sync = new();

    /// <inheritdoc />
    public Task UpsertChunksAsync(IReadOnlyList<RetrievalChunk> chunks, CancellationToken ct)
    {
        lock (_sync)
        {
            foreach (RetrievalChunk chunk in chunks)
            {
                _chunks.RemoveAll(x => x.ChunkId == chunk.ChunkId);
                _chunks.Add(chunk);
            }

            if (_chunks.Count > MaxChunks)
                _chunks.RemoveRange(0, _chunks.Count - MaxChunks);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RetrievalHit>> SearchAsync(
        RetrievalQuery query,
        float[] queryEmbedding,
        CancellationToken ct)
    {
        lock (_sync)
        {
            List<RetrievalHit> hits = _chunks
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

        for (int i = 0; i < a.Length; i++)
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

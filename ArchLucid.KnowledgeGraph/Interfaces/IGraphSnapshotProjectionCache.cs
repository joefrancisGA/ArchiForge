using ArchLucid.Core.Scoping;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Interfaces;

/// <summary>Read-through cache for heavy graph snapshot projections (e.g. full node/edge hydration).</summary>
public interface IGraphSnapshotProjectionCache
{
    /// <summary>
    ///     Returns a cached snapshot or materializes via <paramref name="loadFromStore" />; does not cache
    ///     <see langword="null" /> results.
    /// </summary>
    Task<GraphSnapshot?> GetOrLoadAsync(
        ScopeContext scope,
        Guid runId,
        Guid graphSnapshotId,
        Func<CancellationToken, Task<GraphSnapshot?>> loadFromStore,
        CancellationToken cancellationToken);

    /// <summary>Evicts a projection entry (e.g. after relational backfill or post-authority commit).</summary>
    void Invalidate(ScopeContext scope, Guid runId, Guid graphSnapshotId);
}

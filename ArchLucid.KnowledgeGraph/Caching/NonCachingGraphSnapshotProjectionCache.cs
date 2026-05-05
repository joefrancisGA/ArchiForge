using ArchLucid.Core.Scoping;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Caching;

/// <summary>Pass-through cache for hosts that must not retain graph projections in memory (CLIs, tests).</summary>
public sealed class NonCachingGraphSnapshotProjectionCache : IGraphSnapshotProjectionCache
{
    public static NonCachingGraphSnapshotProjectionCache Instance { get; } = new();

    private NonCachingGraphSnapshotProjectionCache()
    {
    }

    /// <inheritdoc />
    public Task<GraphSnapshot?> GetOrLoadAsync(
        ScopeContext scope,
        Guid runId,
        Guid graphSnapshotId,
        Func<CancellationToken, Task<GraphSnapshot?>> loadFromStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(loadFromStore);

        return loadFromStore(cancellationToken);
    }

    /// <inheritdoc />
    public void Invalidate(ScopeContext scope, Guid runId, Guid graphSnapshotId)
    {
    }
}

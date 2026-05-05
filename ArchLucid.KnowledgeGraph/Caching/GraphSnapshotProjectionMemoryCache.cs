using ArchLucid.Core.Scoping;
using ArchLucid.KnowledgeGraph.Configuration;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.KnowledgeGraph.Caching;

/// <summary><see cref="IMemoryCache" />-backed implementation of <see cref="IGraphSnapshotProjectionCache" />.</summary>
public sealed class GraphSnapshotProjectionMemoryCache(
    IMemoryCache memoryCache,
    IOptionsMonitor<KnowledgeGraphProjectionCacheOptions> optionsMonitor) : IGraphSnapshotProjectionCache
{
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private readonly IOptionsMonitor<KnowledgeGraphProjectionCacheOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public async Task<GraphSnapshot?> GetOrLoadAsync(
        ScopeContext scope,
        Guid runId,
        Guid graphSnapshotId,
        Func<CancellationToken, Task<GraphSnapshot?>> loadFromStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(loadFromStore);

        if (!_optionsMonitor.CurrentValue.Enabled)
            return await loadFromStore(cancellationToken);

        string key = GraphSnapshotProjectionCacheKeys.Projection(scope, runId, graphSnapshotId);

        if (_memoryCache.TryGetValue(key, out object? boxed) && boxed is GraphSnapshot typed)
            return typed;

        GraphSnapshot? created = await loadFromStore(cancellationToken);

        if (created is null)
            return null;

        TimeSpan ttl = ResolveTtl();

        using (ICacheEntry entry = _memoryCache.CreateEntry(key))
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            entry.Value = created;
        }

        return created;
    }

    /// <inheritdoc />
    public void Invalidate(ScopeContext scope, Guid runId, Guid graphSnapshotId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        string key = GraphSnapshotProjectionCacheKeys.Projection(scope, runId, graphSnapshotId);
        _memoryCache.Remove(key);
    }

    private TimeSpan ResolveTtl()
    {
        int seconds = _optionsMonitor.CurrentValue.AbsoluteExpirationSeconds;

        if (seconds < 1)
            seconds = 300;

        seconds = Math.Clamp(seconds, 1, 86400);

        return TimeSpan.FromSeconds(seconds);
    }
}

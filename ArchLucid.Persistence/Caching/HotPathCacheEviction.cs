using ArchLucid.Core.Scoping;

namespace ArchLucid.Persistence.Caching;

/// <summary>Removes both current and legacy hot-path keys after writes (prefix migration from legacy <c>af:hot:</c>).</summary>
public static class HotPathCacheEviction
{
    public static async Task RemoveManifestAsync(
        IHotPathReadCache cache,
        ScopeContext scope,
        Guid manifestId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(scope);

        await cache.RemoveAsync(HotPathCacheKeys.Manifest(scope, manifestId), ct);
        await cache.RemoveAsync(HotPathCacheKeys.LegacyManifest(scope, manifestId), ct);
    }

    public static async Task RemoveRunAsync(IHotPathReadCache cache, ScopeContext scope, Guid runId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(scope);

        await cache.RemoveAsync(HotPathCacheKeys.Run(scope, runId), ct);
        await cache.RemoveAsync(HotPathCacheKeys.LegacyRun(scope, runId), ct);
    }

    public static async Task RemovePolicyPackAsync(IHotPathReadCache cache, Guid policyPackId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cache);

        await cache.RemoveAsync(HotPathCacheKeys.PolicyPack(policyPackId), ct);
        await cache.RemoveAsync(HotPathCacheKeys.LegacyPolicyPack(policyPackId), ct);
    }
}

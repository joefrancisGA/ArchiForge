namespace ArchLucid.Persistence.Caching;

/// <summary>Small abstraction over memory or distributed cache for read-through hot paths.</summary>
public interface IHotPathReadCache
{
    /// <summary>
    ///     Returns a cached instance or materializes via <paramref name="factory" />; does not cache
    ///     <see langword="null" /> results.
    /// </summary>
    /// <param name="legacyCacheKey">
    ///     Optional former key (e.g. hot-path prefix migration); when present, a hit promotes into
    ///     <paramref name="key" />.
    /// </param>
    /// <param name="absoluteExpirationSecondsOverride">
    ///     When set, overrides
    ///     <see cref="ArchLucid.Persistence.Coordination.Caching.HotPathCacheOptions.AbsoluteExpirationSeconds" /> for this
    ///     entry only (clamped by the implementation).
    /// </param>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct,
        string? legacyCacheKey = null,
        int? absoluteExpirationSecondsOverride = null)
        where T : class;

    /// <summary>Removes one key (e.g. after a successful write).</summary>
    Task RemoveAsync(string key, CancellationToken ct);
}

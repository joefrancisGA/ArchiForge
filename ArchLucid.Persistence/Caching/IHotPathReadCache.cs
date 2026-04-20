namespace ArchLucid.Persistence.Caching;

/// <summary>Small abstraction over memory or distributed cache for read-through hot paths.</summary>
public interface IHotPathReadCache
{
    /// <summary>Returns a cached instance or materializes via <paramref name="factory"/>; does not cache <see langword="null"/> results.</summary>
    /// <param name="legacyCacheKey">Optional former key (e.g. hot-path prefix migration); when present, a hit promotes into <paramref name="key"/>.</param>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct,
        string? legacyCacheKey = null)
        where T : class;

    /// <summary>Removes one key (e.g. after a successful write).</summary>
    Task RemoveAsync(string key, CancellationToken ct);
}

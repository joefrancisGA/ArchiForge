using ArchLucid.Persistence.Coordination.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Caching;

/// <summary>In-process <see cref="IMemoryCache"/> implementation of <see cref="IHotPathReadCache"/>.</summary>
public sealed class MemoryHotPathReadCache(
    IMemoryCache memoryCache,
    IOptionsMonitor<HotPathCacheOptions> optionsMonitor) : IHotPathReadCache
{
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    private readonly IOptionsMonitor<HotPathCacheOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_memoryCache.TryGetValue(key, out object? boxed) && boxed is T typed)
            return Task.FromResult<T?>(typed);

        return MaterializeAsync(key, factory, ct);
    }

    private async Task<T?> MaterializeAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct)
        where T : class
    {
        T? created = await factory(ct);

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

    private TimeSpan ResolveTtl()
    {
        int seconds = _optionsMonitor.CurrentValue.AbsoluteExpirationSeconds;

        if (seconds < 1)
            seconds = 60;

        seconds = Math.Clamp(seconds, 1, 3600);

        return TimeSpan.FromSeconds(seconds);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}

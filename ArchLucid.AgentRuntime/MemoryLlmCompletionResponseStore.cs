using Microsoft.Extensions.Caching.Memory;

namespace ArchiForge.AgentRuntime;

/// <summary>In-process <see cref="MemoryCache"/> store with bounded entry count.</summary>
public sealed class MemoryLlmCompletionResponseStore : ILlmCompletionResponseStore, IDisposable
{
    private readonly MemoryCache _cache;

    public MemoryLlmCompletionResponseStore(int maxEntries)
    {
        if (maxEntries < 1)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Must be at least 1.");

        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = maxEntries });
    }

    /// <inheritdoc />
    public Task<string?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_cache.TryGetValue(key, out object? cached) && cached is string { Length: > 0 } hit)
            return Task.FromResult<string?>(hit);

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan absoluteExpiration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        if (absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration));

        MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            Size = 1,
        };

        _cache.Set(key, value, options);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _cache.Dispose();
}

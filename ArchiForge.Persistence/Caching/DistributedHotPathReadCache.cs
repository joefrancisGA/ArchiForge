using System.Text.Json;

using ArchiForge.Persistence.Serialization;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.Persistence.Caching;

/// <summary><see cref="IDistributedCache"/> (e.g. Redis) implementation with JSON payloads aligned to <see cref="JsonEntitySerializer"/>.</summary>
public sealed class DistributedHotPathReadCache(
    IDistributedCache distributedCache,
    IOptionsMonitor<HotPathCacheOptions> optionsMonitor,
    ILogger<DistributedHotPathReadCache> logger) : IHotPathReadCache
{
    private readonly IDistributedCache _distributedCache =
        distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));

    private readonly IOptionsMonitor<HotPathCacheOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly ILogger<DistributedHotPathReadCache> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        byte[]? bytes = await _distributedCache.GetAsync(key, ct);

        if (bytes is { Length: > 0 })
        {
            try
            {
                T? fromRedis = JsonSerializer.Deserialize<T>(bytes, JsonEntitySerializer.EntityJsonOptions);

                if (fromRedis is not null)
                    return fromRedis;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "HotPath distributed cache entry for key {CacheKey} is corrupt; refreshing.", key);
            }
        }

        T? created = await factory(ct);

        if (created is null)
            return null;

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(created, JsonEntitySerializer.EntityJsonOptions);
        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = ResolveTtl()
        };

        await _distributedCache.SetAsync(key, payload, entryOptions, ct);

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
    public Task RemoveAsync(string key, CancellationToken ct) => _distributedCache.RemoveAsync(key, ct);
}

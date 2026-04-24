using System.Text.Json;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Coordination.Caching;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Caching;

/// <summary>
///     <see cref="IDistributedCache" /> (e.g. Redis) implementation with JSON payloads aligned to
///     <see cref="JsonEntitySerializer" />.
/// </summary>
public sealed class DistributedHotPathReadCache(
    IDistributedCache distributedCache,
    IOptionsMonitor<HotPathCacheOptions> optionsMonitor,
    ILogger<DistributedHotPathReadCache> logger) : IHotPathReadCache
{
    private readonly IDistributedCache _distributedCache =
        distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));

    private readonly ILogger<DistributedHotPathReadCache> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IOptionsMonitor<HotPathCacheOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct,
        string? legacyCacheKey = null,
        int? absoluteExpirationSecondsOverride = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        T? fromPrimary = await TryDeserializeAsync<T>(key, ct);

        if (fromPrimary is not null)
            return fromPrimary;

        if (legacyCacheKey is not null)
        {
            T? fromLegacy = await TryDeserializeAsync<T>(legacyCacheKey, ct);

            if (fromLegacy is not null)
            {
                await PromoteLegacyToPrimaryAsync(key, legacyCacheKey, fromLegacy, ct,
                    absoluteExpirationSecondsOverride);

                return fromLegacy;
            }
        }

        T? created = await factory(ct);

        if (created is null)
            return null;

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(created, JsonEntitySerializer.EntityJsonOptions);
        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = ResolveTtl(absoluteExpirationSecondsOverride)
        };

        await _distributedCache.SetAsync(key, payload, entryOptions, ct);

        return created;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct)
    {
        return _distributedCache.RemoveAsync(key, ct);
    }

    private async Task<T?> TryDeserializeAsync<T>(string cacheKey, CancellationToken ct)
        where T : class
    {
        byte[]? bytes = await _distributedCache.GetAsync(cacheKey, ct);

        if (bytes is not { Length: > 0 })
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, JsonEntitySerializer.EntityJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "HotPath distributed cache entry for key {CacheKey} is corrupt; refreshing.",
                LogSanitizer.Sanitize(cacheKey));

            return null;
        }
    }

    private async Task PromoteLegacyToPrimaryAsync<T>(
        string primaryKey,
        string legacyKey,
        T value,
        CancellationToken ct,
        int? absoluteExpirationSecondsOverride)
        where T : class
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonEntitySerializer.EntityJsonOptions);
        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = ResolveTtl(absoluteExpirationSecondsOverride)
        };

        await _distributedCache.SetAsync(primaryKey, payload, entryOptions, ct);
        await _distributedCache.RemoveAsync(legacyKey, ct);
    }

    private TimeSpan ResolveTtl(int? absoluteExpirationSecondsOverride)
    {
        int seconds = absoluteExpirationSecondsOverride ?? _optionsMonitor.CurrentValue.AbsoluteExpirationSeconds;

        if (seconds < 1)
            seconds = 60;

        seconds = Math.Clamp(seconds, 1, 3600);

        return TimeSpan.FromSeconds(seconds);
    }
}

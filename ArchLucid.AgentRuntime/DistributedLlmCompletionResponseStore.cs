using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace ArchiForge.AgentRuntime;

/// <summary>Redis (or any <see cref="IDistributedCache"/>) store for cross-replica LLM response reuse.</summary>
public sealed class DistributedLlmCompletionResponseStore(IDistributedCache distributedCache) : ILlmCompletionResponseStore
{
    private readonly IDistributedCache _distributedCache =
        distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));

    /// <inheritdoc />
    public async Task<string?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[]? bytes = await _distributedCache.GetAsync(key, cancellationToken);

        if (bytes is null || bytes.Length == 0)
            return null;

        try
        {
            return JsonSerializer.Deserialize<string>(bytes);
        }
        catch (JsonException)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);

            return null;
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan absoluteExpiration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        if (absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration));

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value);
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
        };

        return _distributedCache.SetAsync(key, payload, options, cancellationToken);
    }
}

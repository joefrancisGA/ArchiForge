using ArchiForge.Core.Scoping;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Decorator that caches assistant JSON bodies for identical prompt pairs (and optional scope partition).
/// </summary>
/// <remarks>
/// Sits <em>inside</em> <see cref="CircuitBreakingAgentCompletionClient"/> so cache hits avoid Azure and do not affect the breaker.
/// Uses a dedicated <see cref="MemoryCache"/> with a size limit to cap memory.
/// </remarks>
public sealed class CachingAgentCompletionClient : IAgentCompletionClient, IDisposable
{
    private const string CacheKeyPrefix = "llm:completion:v1:";

    private readonly IAgentCompletionClient _inner;

    private readonly MemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly bool _enabled;
    private readonly bool _partitionByScope;
    private readonly string _deploymentName;
    private readonly IScopeContextProvider _scopeProvider;
    private readonly ILogger<CachingAgentCompletionClient> _logger;

    /// <summary>
    /// Creates a caching wrapper around <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">Downstream client (typically Azure OpenAI).</param>
    /// <param name="deploymentName">Chat deployment name; included in the cache key.</param>
    /// <param name="enabled">When false, every call is forwarded.</param>
    /// <param name="partitionByScope">When true, <see cref="IScopeContextProvider"/> ids are mixed into the key.</param>
    /// <param name="absoluteExpiration">TTL for cached entries.</param>
    /// <param name="maxEntries">Memory cache capacity (uniform entry size).</param>
    /// <param name="scopeProvider">Current HTTP or ambient scope; required when <paramref name="partitionByScope"/> is true.</param>
    /// <param name="logger">Logger.</param>
    public CachingAgentCompletionClient(
        IAgentCompletionClient inner,
        string deploymentName,
        bool enabled,
        bool partitionByScope,
        TimeSpan absoluteExpiration,
        int maxEntries,
        IScopeContextProvider scopeProvider,
        ILogger<CachingAgentCompletionClient> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        if (maxEntries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "MaxEntries must be at least 1.");
        }

        _inner = inner;
        _deploymentName = deploymentName;
        _enabled = enabled;
        _partitionByScope = partitionByScope;
        _ttl = absoluteExpiration > TimeSpan.Zero
            ? absoluteExpiration
            : throw new ArgumentOutOfRangeException(nameof(absoluteExpiration));
        _scopeProvider = scopeProvider;
        _logger = logger;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = maxEntries
        });
    }

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            return await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
        }

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        string key =
            CacheKeyPrefix
            + LlmCompletionCacheKey.Compute(
                _partitionByScope,
                _deploymentName,
                systemPrompt,
                userPrompt,
                scope);

        if (_cache.TryGetValue(key, out object? cached) && cached is string hit && hit.Length > 0)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("LLM completion cache hit (key prefix {KeyPrefix}).", key[..Math.Min(24, key.Length)]);
            }

            return hit;
        }

        string result = await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        MemoryCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _ttl,
            Size = 1
        };

        _cache.Set(key, result, entryOptions);

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }
}

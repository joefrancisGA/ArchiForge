using ArchiForge.Core.Scoping;

using Microsoft.Extensions.Logging;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Decorator that caches assistant JSON bodies for identical prompt pairs (and optional scope partition).
/// </summary>
/// <remarks>
/// Sits <em>inside</em> <see cref="CircuitBreakingAgentCompletionClient"/> so cache hits avoid Azure and do not affect the breaker.
/// Backing store is <see cref="ILlmCompletionResponseStore"/> (memory or distributed Redis).
/// </remarks>
public sealed class CachingAgentCompletionClient : IAgentCompletionClient
{
    private const string CacheKeyPrefix = "llm:completion:v1:";

    private readonly IAgentCompletionClient _inner;
    private readonly ILlmCompletionResponseStore _store;
    private readonly TimeSpan _ttl;
    private readonly bool _enabled;
    private readonly bool _partitionByScope;
    private readonly string _deploymentName;
    private readonly IScopeContextProvider _scopeProvider;
    private readonly ILogger<CachingAgentCompletionClient> _logger;

    /// <summary>Creates a caching wrapper around <paramref name="inner"/>.</summary>
    public CachingAgentCompletionClient(
        IAgentCompletionClient inner,
        ILlmCompletionResponseStore store,
        string deploymentName,
        bool enabled,
        bool partitionByScope,
        TimeSpan absoluteExpiration,
        IScopeContextProvider scopeProvider,
        ILogger<CachingAgentCompletionClient> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _store = store;
        _deploymentName = deploymentName;
        _enabled = enabled;
        _partitionByScope = partitionByScope;
        _ttl = absoluteExpiration > TimeSpan.Zero
            ? absoluteExpiration
            : throw new ArgumentOutOfRangeException(nameof(absoluteExpiration));
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled)
            return await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        string key =
            CacheKeyPrefix
            + LlmCompletionCacheKey.Compute(
                _partitionByScope,
                _deploymentName,
                systemPrompt,
                userPrompt,
                scope);

        string? cached = await _store.TryGetAsync(key, cancellationToken);

        if (cached is { Length: > 0 })
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("LLM completion cache hit (key prefix {KeyPrefix}).", key[..Math.Min(24, key.Length)]);
            }

            return cached;
        }

        string result = await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);

        await _store.SetAsync(key, result, _ttl, cancellationToken);

        return result;
    }
}

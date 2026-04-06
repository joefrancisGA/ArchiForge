using ArchiForge.Core.Resilience;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Retrieval.Embedding;

/// <summary>
/// Decorator for <see cref="IOpenAiEmbeddingClient"/> that applies a <see cref="CircuitBreakerGate"/> around embedding calls.
/// </summary>
public sealed class CircuitBreakingOpenAiEmbeddingClient(
    IOpenAiEmbeddingClient inner,
    CircuitBreakerGate gate,
    ILogger<CircuitBreakingOpenAiEmbeddingClient> logger) : IOpenAiEmbeddingClient
{
    private readonly IOpenAiEmbeddingClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly CircuitBreakerGate _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    private readonly ILogger<CircuitBreakingOpenAiEmbeddingClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        _gate.ThrowIfBroken();

        try
        {
            float[] result = await _inner.EmbedAsync(text, ct);
            _gate.RecordSuccess();
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _gate.RecordCallCancelled();
            throw;
        }
        catch (Exception ex)
        {
            _gate.RecordFailure();
            _logger.LogWarning(ex, "Azure OpenAI embedding call failed; circuit breaker recorded failure.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        _gate.ThrowIfBroken();

        try
        {
            IReadOnlyList<float[]> result = await _inner.EmbedManyAsync(texts, ct);
            _gate.RecordSuccess();
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _gate.RecordCallCancelled();
            throw;
        }
        catch (Exception ex)
        {
            _gate.RecordFailure();
            _logger.LogWarning(ex, "Azure OpenAI embedding batch failed; circuit breaker recorded failure.");
            throw;
        }
    }
}

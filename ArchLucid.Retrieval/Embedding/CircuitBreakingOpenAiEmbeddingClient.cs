using ArchLucid.Core.Resilience;

using Microsoft.Extensions.Logging;

using Polly;

namespace ArchLucid.Retrieval.Embedding;

/// <summary>
///     Decorator for <see cref="IOpenAiEmbeddingClient" /> that applies a <see cref="CircuitBreakerGate" /> around
///     embedding calls.
/// </summary>
public sealed class CircuitBreakingOpenAiEmbeddingClient(
    IOpenAiEmbeddingClient inner,
    CircuitBreakerGate gate,
    ResiliencePipeline llmRetryPipeline,
    ILogger<CircuitBreakingOpenAiEmbeddingClient> logger) : IOpenAiEmbeddingClient
{
    private readonly CircuitBreakerGate _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    private readonly IOpenAiEmbeddingClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly ResiliencePipeline _llmRetryPipeline =
        llmRetryPipeline ?? throw new ArgumentNullException(nameof(llmRetryPipeline));

    private readonly ILogger<CircuitBreakingOpenAiEmbeddingClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        _gate.ThrowIfBroken();

        try
        {
            ct.ThrowIfCancellationRequested();

            float[] result = await _llmRetryPipeline.ExecuteAsync(
                async innerCt => await _inner.EmbedAsync(text, innerCt),
                ct);

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
            _logger.LogWarning(ex,
                "Azure OpenAI embedding call failed after retries; circuit breaker recorded failure.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        _gate.ThrowIfBroken();

        try
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<float[]> result = await _llmRetryPipeline.ExecuteAsync(
                async innerCt => await _inner.EmbedManyAsync(texts, innerCt),
                ct);

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
            _logger.LogWarning(ex,
                "Azure OpenAI embedding batch failed after retries; circuit breaker recorded failure.");
            throw;
        }
    }
}

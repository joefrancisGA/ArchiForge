using ArchLucid.Core.Resilience;

using Microsoft.Extensions.Logging;

using Polly;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Decorator for <see cref="IAgentCompletionClient"/> that applies a <see cref="CircuitBreakerGate"/> around Azure OpenAI calls.
/// </summary>
public sealed class CircuitBreakingAgentCompletionClient(
    IAgentCompletionClient inner,
    CircuitBreakerGate gate,
    ResiliencePipeline llmRetryPipeline,
    ILogger<CircuitBreakingAgentCompletionClient> logger) : IAgentCompletionClient, IDisposable
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly CircuitBreakerGate _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    private readonly ResiliencePipeline _llmRetryPipeline =
        llmRetryPipeline ?? throw new ArgumentNullException(nameof(llmRetryPipeline));
    private readonly ILogger<CircuitBreakingAgentCompletionClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _inner.Descriptor;

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _gate.ThrowIfBroken();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string result = await _llmRetryPipeline.ExecuteAsync(
                async ct => await _inner.CompleteJsonAsync(systemPrompt, userPrompt, ct),
                cancellationToken);

            _gate.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _gate.RecordCallCancelled();
            throw;
        }
        catch (Exception ex)
        {
            _gate.RecordFailure();
            _logger.LogWarning(ex, "LLM completion call failed after retries; circuit breaker recorded failure.");
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_inner is IDisposable disposableInner)
        {
            disposableInner.Dispose();
        }
    }
}

using ArchiForge.Core.Resilience;

using Microsoft.Extensions.Logging;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Decorator for <see cref="IAgentCompletionClient"/> that applies a <see cref="CircuitBreakerGate"/> around Azure OpenAI calls.
/// </summary>
public sealed class CircuitBreakingAgentCompletionClient(
    IAgentCompletionClient inner,
    CircuitBreakerGate gate,
    ILogger<CircuitBreakingAgentCompletionClient> logger) : IAgentCompletionClient, IDisposable
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly CircuitBreakerGate _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    private readonly ILogger<CircuitBreakingAgentCompletionClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _gate.ThrowIfBroken();

        try
        {
            string result = await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken)
                ;
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
            _logger.LogWarning(ex, "Azure OpenAI completion call failed; circuit breaker recorded failure.");
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

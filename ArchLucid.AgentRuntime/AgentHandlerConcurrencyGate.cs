using Microsoft.Extensions.Options;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Semaphore-based bulkhead for <see cref="RealAgentExecutor"/> — caps parallel OpenAI-backed handlers.
/// </summary>
public sealed class AgentHandlerConcurrencyGate : IAgentHandlerConcurrencyGate
{
    private readonly SemaphoreSlim? _semaphore;

    public AgentHandlerConcurrencyGate(IOptions<AgentExecutionResilienceOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        int max = options.Value.MaxConcurrentHandlers;

        if (max > 0)
        {
            _semaphore = new SemaphoreSlim(max, max);
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_semaphore is null)
        {
            return await action(cancellationToken);
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Bulkhead-style limiter for concurrent agent handler executions (shared process-wide).
/// </summary>
public interface IAgentHandlerConcurrencyGate
{
    /// <summary>Runs <paramref name="action"/> under the configured concurrency limit.</summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Limits concurrent LLM-backed agent handlers and caps per-handler wall time (Polly timeout).
/// </summary>
/// <remarks>
/// Bound from configuration under <c>AgentExecution:Resilience</c>.
/// <see cref="MaxConcurrentHandlers"/> ≤ 0 disables the bulkhead (unlimited parallelism — tests only).
/// <see cref="PerHandlerTimeoutSeconds"/> ≤ 0 disables the timeout pipeline.
/// </remarks>
public sealed class AgentExecutionResilienceOptions
{
    public const string SectionName = "AgentExecution:Resilience";

    /// <summary>Maximum agent handlers executing LLM work at once across the process (singleton gate). Default 8.</summary>
    public int MaxConcurrentHandlers { get; set; } = 8;

    /// <summary>Per-handler wall-clock timeout in seconds. Default 900 (15 minutes). 0 = disabled.</summary>
    public int PerHandlerTimeoutSeconds { get; set; } = 900;
}

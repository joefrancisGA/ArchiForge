namespace ArchLucid.Contracts.Agents;

/// <summary>
///     Stable, low-cardinality codes recorded on <see cref="AgentExecutionTrace.FailureReasonCode" /> when a trace row
///     captures a failure path that operators may want to alert on (distinct from free-text <see cref="AgentExecutionTrace.ErrorMessage" />).
/// </summary>
public static class AgentExecutionTraceFailureReasonCodes
{
    /// <summary>LLM call was rejected because the completion circuit gate was open or a recovery probe was in flight.</summary>
    public const string CircuitBreakerRejected = nameof(CircuitBreakerRejected);

    /// <summary>
    ///     LLM call was rejected before dispatch because per-tenant sliding-window token quota or UTC-day budget would be
    ///     exceeded.
    /// </summary>
    public const string LlmTokenQuotaExceeded = nameof(LlmTokenQuotaExceeded);
}

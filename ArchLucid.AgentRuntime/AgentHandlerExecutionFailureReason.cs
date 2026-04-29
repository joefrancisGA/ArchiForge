using ArchLucid.Contracts.Agents;

using ArchLucid.Core.Resilience;

namespace ArchLucid.AgentRuntime;

internal static class AgentHandlerExecutionFailureReason
{
    /// <summary>
    ///     Maps a caught exception to a persisted <see cref="AgentExecutionTrace.FailureReasonCode" /> when the failure
    ///     class is stable enough for alerting (<see langword="null" /> for generic failures).
    /// </summary>
    internal static string? ResolveFailureReasonCode(Exception ex)
    {
        if (ex is null)
            throw new ArgumentNullException(nameof(ex));

        if (ex is CircuitBreakerOpenException)
            return AgentExecutionTraceFailureReasonCodes.CircuitBreakerRejected;

        return null;
    }
}

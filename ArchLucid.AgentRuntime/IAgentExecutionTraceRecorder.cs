using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Persists an <see cref="ArchLucid.Contracts.Agents.AgentExecutionTrace" /> after each model call (success or parse
///     failure).
/// </summary>
public interface IAgentExecutionTraceRecorder
{
    /// <summary>
    ///     Writes one trace row for auditing and analysis exports.
    /// </summary>
    /// <param name="runId">Owning run.</param>
    /// <param name="taskId">Owning agent task.</param>
    /// <param name="agentType">Agent role.</param>
    /// <param name="systemPrompt">Prompt sent to the model (may be truncated by the implementation).</param>
    /// <param name="userPrompt">User message sent to the model (may be truncated).</param>
    /// <param name="rawResponse">Raw model output.</param>
    /// <param name="parsedResultJson">Serialized structured result when parsing succeeded; otherwise <see langword="null" />.</param>
    /// <param name="parseSucceeded">Whether <paramref name="parsedResultJson" /> is usable.</param>
    /// <param name="errorMessage">
    ///     Parse or validation error text when <paramref name="parseSucceeded" /> is
    ///     <see langword="false" />.
    /// </param>
    /// <param name="promptRepro">
    ///     Template id/version/hash and optional release label; <see langword="null" /> when not
    ///     applicable.
    /// </param>
    /// <param name="inputTokenCount">Provider-reported prompt tokens, when known.</param>
    /// <param name="outputTokenCount">Provider-reported completion tokens, when known.</param>
    /// <param name="modelDeploymentName">Provider deployment name when known (stored on the trace row).</param>
    /// <param name="modelVersion">Provider model version string when known.</param>
    /// <param name="isSimulatorExecution">
    ///     When <see langword="true" /> (simulator / deterministic executor), full-text blob + SQL inline persistence is
    ///     skipped;
    ///     truncated columns on the trace row remain the forensic surface. Real LLM handlers pass <see langword="false" />
    ///     (default).
    /// </param>
    /// <param name="failureReasonCode">
    ///     Optional stable code (e.g. <see cref="AgentExecutionTraceFailureReasonCodes.CircuitBreakerRejected" />) for
    ///     operator alerting; persisted on <see cref="AgentExecutionTrace.FailureReasonCode" />.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the trace is stored.</returns>
    Task RecordAsync(
        string runId,
        string taskId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        string? parsedResultJson,
        bool parseSucceeded,
        string? errorMessage,
        AgentPromptReproMetadata? promptRepro = null,
        int? inputTokenCount = null,
        int? outputTokenCount = null,
        string? modelDeploymentName = null,
        string? modelVersion = null,
        bool isSimulatorExecution = false,
        string? failureReasonCode = null,
        CancellationToken cancellationToken = default);
}

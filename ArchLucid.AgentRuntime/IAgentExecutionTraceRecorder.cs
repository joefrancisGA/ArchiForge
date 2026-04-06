using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Persists an <see cref="ArchLucid.Contracts.Agents.AgentExecutionTrace"/> after each model call (success or parse failure).
/// </summary>
public interface IAgentExecutionTraceRecorder
{
    /// <summary>
    /// Writes one trace row for auditing and analysis exports.
    /// </summary>
    /// <param name="runId">Owning run.</param>
    /// <param name="taskId">Owning agent task.</param>
    /// <param name="agentType">Agent role.</param>
    /// <param name="systemPrompt">Prompt sent to the model (may be truncated by the implementation).</param>
    /// <param name="userPrompt">User message sent to the model (may be truncated).</param>
    /// <param name="rawResponse">Raw model output.</param>
    /// <param name="parsedResultJson">Serialized structured result when parsing succeeded; otherwise <see langword="null"/>.</param>
    /// <param name="parseSucceeded">Whether <paramref name="parsedResultJson"/> is usable.</param>
    /// <param name="errorMessage">Parse or validation error text when <paramref name="parseSucceeded"/> is <see langword="false"/>.</param>
    /// <param name="promptRepro">Template id/version/hash and optional release label; <see langword="null"/> when not applicable.</param>
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
        CancellationToken cancellationToken = default);
}

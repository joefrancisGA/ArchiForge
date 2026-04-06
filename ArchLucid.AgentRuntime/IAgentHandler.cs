using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Executes one <see cref="AgentTask"/> for a run: builds prompts from <see cref="ArchitectureRequest"/> and
/// <see cref="AgentEvidencePackage"/>, calls the completion client, parses <see cref="AgentResult"/>, and records traces.
/// </summary>
/// <remarks>Registered per <see cref="ArchiForge.Contracts.Common.AgentType"/>; <see cref="RealAgentExecutor"/> dispatches by task type.</remarks>
public interface IAgentHandler
{
    /// <summary>Agent role this handler implements.</summary>
    AgentType AgentType { get; }

    /// <summary>Stable key used for DI registration and <see cref="AgentTask.AgentTypeKey"/> dispatch (e.g. <c>topology</c>).</summary>
    string AgentTypeKey { get; }

    /// <summary>
    /// Runs the LLM (or simulator) pipeline for <paramref name="task"/> and returns a validated <see cref="AgentResult"/>.
    /// </summary>
    /// <param name="runId">Run identifier (must match <paramref name="task"/>.<see cref="AgentTask.RunId"/>).</param>
    /// <param name="request">Original architecture request.</param>
    /// <param name="evidence">Hydrated evidence package for the run.</param>
    /// <param name="task">Task to execute (objective, allowed tools/sources).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes with the agent result for persistence and decisioning.</returns>
    Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task,
        CancellationToken cancellationToken = default);
}

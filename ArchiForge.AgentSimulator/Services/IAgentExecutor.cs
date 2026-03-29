using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentSimulator.Services;

/// <summary>
/// Runs a batch of <see cref="AgentTask"/> items for one architecture run (production handlers or test doubles).
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Executes each task in <paramref name="tasks"/> in agent-type order and collects <see cref="AgentResult"/> rows.
    /// </summary>
    /// <param name="runId">Run identifier shared by all tasks.</param>
    /// <param name="request">Architecture request supplied to every handler.</param>
    /// <param name="evidence">Evidence package shared across tasks.</param>
    /// <param name="tasks">Non-empty set of tasks to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that resolves to ordered results (one per task).</returns>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="IAgentHandler"/> is registered for a task’s agent type.</exception>
    Task<IReadOnlyList<AgentResult>> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        CancellationToken cancellationToken = default);
}

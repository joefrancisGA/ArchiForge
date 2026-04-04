using ArchiForge.Contracts.Agents;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="AgentTask"/> records associated with architecture runs.
/// </summary>
public interface IAgentTaskRepository
{
    /// <summary>
    /// Persists multiple tasks in a single batch operation.
    /// Each task in <paramref name="tasks"/> must have a unique <c>TaskId</c>.
    /// Implementors should treat each write as an insert; duplicate IDs result in implementation-defined behaviour.
    /// </summary>
    Task CreateManyAsync(IEnumerable<AgentTask> tasks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all tasks for the given <paramref name="runId"/>, ordered by creation time ascending.
    /// Returns an empty list (never <see langword="null"/>) when no tasks are found.
    /// </summary>
    Task<IReadOnlyList<AgentTask>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

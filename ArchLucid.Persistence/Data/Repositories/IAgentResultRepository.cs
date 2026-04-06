using ArchiForge.Contracts.Agents;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence interface for <see cref="AgentResult"/> records produced during an architecture run.
/// </summary>
public interface IAgentResultRepository
{
    /// <summary>Persists a single agent result.</summary>
    /// <param name="result">The result to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(AgentResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists multiple agent results in a single operation.
    /// Implementations should use an idempotent delete-then-insert strategy within a transaction
    /// to allow safe retries.
    /// </summary>
    /// <param name="results">The results to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateManyAsync(IReadOnlyList<AgentResult> results, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all agent results for the specified run, in creation order.
    /// Returns an empty collection when the run exists but has no results yet.
    /// </summary>
    /// <param name="runId">The run identifier to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AgentResult>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

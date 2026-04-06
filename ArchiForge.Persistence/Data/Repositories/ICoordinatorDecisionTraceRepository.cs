using ArchiForge.Contracts.DecisionTraces;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for coordinator <see cref="DecisionTrace"/> rows (merge/engine event log for string runs).
/// </summary>
public interface ICoordinatorDecisionTraceRepository
{
    /// <summary>
    /// Persists multiple decision traces in a single batch operation.
    /// Each trace must have <see cref="DecisionTrace.Kind"/> <see cref="DecisionTraceKind.RunEvent"/> and a unique
    /// <c>RunEvent.TraceId</c>.
    /// </summary>
    Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all decision traces for the given <paramref name="runId"/>, ordered by creation time ascending.
    /// Returns an empty list (never <see langword="null"/>) when no traces are found.
    /// </summary>
    Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

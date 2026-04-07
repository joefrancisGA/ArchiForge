using System.Data;

using ArchLucid.Contracts.DecisionTraces;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for coordinator <see cref="RunEventTrace"/> rows (merge/engine event log for string runs).
/// </summary>
public interface ICoordinatorDecisionTraceRepository
{
    /// <summary>
    /// Persists multiple decision traces in a single batch operation.
    /// Each trace must be a <see cref="RunEventTrace"/> (<see cref="DecisionTraceKind.RunEvent"/>) with a unique
    /// <c>RunEvent.TraceId</c>.
    /// </summary>
    Task CreateManyAsync(
        IEnumerable<DecisionTrace> traces,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns all decision traces for the given <paramref name="runId"/>, ordered by creation time ascending.
    /// Returns an empty list (never <see langword="null"/>) when no traces are found.
    /// </summary>
    Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

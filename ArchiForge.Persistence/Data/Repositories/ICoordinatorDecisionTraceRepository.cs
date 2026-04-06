using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for coordinator <see cref="RunEventTrace"/> rows (merge/engine event log for string runs).
/// </summary>
public interface ICoordinatorDecisionTraceRepository
{
    /// <summary>
    /// Persists multiple decision traces in a single batch operation.
    /// Each trace in <paramref name="traces"/> must have a unique <c>TraceId</c>.
    /// </summary>
    Task CreateManyAsync(IEnumerable<RunEventTrace> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all decision traces for the given <paramref name="runId"/>, ordered by creation time ascending.
    /// Returns an empty list (never <see langword="null"/>) when no traces are found.
    /// </summary>
    Task<IReadOnlyList<RunEventTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

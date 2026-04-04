using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="DecisionTrace"/> records produced by the decision engine during a run.
/// </summary>
public interface IDecisionTraceRepository
{
    /// <summary>
    /// Persists multiple decision traces in a single batch operation.
    /// Each trace in <paramref name="traces"/> must have a unique <c>TraceId</c>.
    /// </summary>
    Task CreateManyAsync(IEnumerable<DecisionTrace> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all decision traces for the given <paramref name="runId"/>, ordered by creation time ascending.
    /// Returns an empty list (never <see langword="null"/>) when no traces are found.
    /// </summary>
    Task<IReadOnlyList<DecisionTrace>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

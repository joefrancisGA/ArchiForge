using System.Data;

using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Persistence contract for <see cref="DecisionNode" /> records produced by the decision
///     engine as part of the architecture run evaluation.
/// </summary>
public interface IDecisionNodeRepository
{
    /// <summary>Persists a batch of decision nodes atomically.</summary>
    /// <param name="decisions">The nodes to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateManyAsync(
        IReadOnlyCollection<DecisionNode> decisions,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Returns all decision nodes for the specified run, ordered by their sequence/position.
    /// </summary>
    /// <param name="runId">The run whose decision nodes are requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<DecisionNode>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);
}

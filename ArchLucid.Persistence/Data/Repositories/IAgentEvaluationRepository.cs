using System.Data;

using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
/// Persistence contract for <see cref="AgentEvaluation"/> records that capture
/// inter-agent assessments (support, oppose, caution, strengthen) produced during a run.
/// </summary>
public interface IAgentEvaluationRepository
{
    /// <summary>Persists a batch of agent evaluations atomically.</summary>
    /// <param name="evaluations">The evaluations to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task CreateManyAsync(
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    /// Returns all evaluations recorded for the specified run, ordered by creation time ascending.
    /// </summary>
    /// <param name="runId">The run whose evaluations are requested.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<AgentEvaluation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default);
}


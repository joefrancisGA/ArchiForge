using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.Decisioning.Merge;

/// <summary>
/// Deterministic weighted-argument decision resolver (v2 scoring model).
/// Produces <see cref="DecisionNode"/> instances that are consumed by <see cref="IDecisionEngineService.MergeResults"/>
/// to finalize topology acceptance, security control promotion, and complexity disposition.
/// </summary>
public interface IDecisionEngineV2
{
    /// <summary>
    /// Resolves a set of <see cref="DecisionNode"/> records for the given run inputs.
    /// </summary>
    /// <param name="runId">The identifier of the run. Must not be blank.</param>
    /// <param name="request">The original architecture request.</param>
    /// <param name="tasks">All agent tasks for the run.</param>
    /// <param name="results">All agent results for the run.</param>
    /// <param name="evaluations">All peer evaluation signals for the run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of resolved <see cref="DecisionNode"/> instances. Returns an empty list (never <see langword="null"/>)
    /// when no topology task or result is present, which is a valid signal that decision nodes cannot be resolved yet.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/>, <paramref name="tasks"/>, <paramref name="results"/>,
    /// or <paramref name="evaluations"/> is <see langword="null"/>.
    /// </exception>
    Task<IReadOnlyList<DecisionNode>> ResolveAsync(
        string runId,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default);
}

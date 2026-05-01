using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Decisions;

/// <summary>
///     Produces lightweight cross-agent evaluations that support or oppose the proposals
///     made by other agents, adjusting confidence scores during manifest synthesis.
/// </summary>
public interface IAgentEvaluationService
{
    /// <summary>
    ///     Evaluates the results produced by all agents in a run and returns a set of
    ///     <see cref="AgentEvaluation" /> records that reinforce or challenge individual proposals.
    /// </summary>
    /// <param name="runId">The run being evaluated.</param>
    /// <param name="request">The originating architecture request supplying context.</param>
    /// <param name="evidence">The evidence package assembled for this run.</param>
    /// <param name="tasks">All agent tasks in the run.</param>
    /// <param name="results">All agent results to be cross-evaluated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A read-only list of evaluations. Implementations may return an empty list when
    ///     no cross-agent adjustments are warranted; they must not throw when inputs are valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when any non-optional argument is <see langword="null" />.
    /// </exception>
    Task<IReadOnlyList<AgentEvaluation>> EvaluateAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        CancellationToken cancellationToken = default);
}

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Inputs shared by <see cref="IDecisionStrategy" /> implementations for a single resolve pass.
/// </summary>
public sealed class DecisionStrategyParameters
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DecisionStrategyParameters" /> class.
    /// </summary>
    /// <param name="runId">Run identifier copied to emitted <see cref="DecisionNode.RunId" />.</param>
    /// <param name="clock">Source of UTC timestamps for <see cref="DecisionNode.CreatedUtc" />.</param>
    /// <param name="evaluations">All evaluations considered or filtered by each strategy.</param>
    /// <param name="tasks">
    ///     Task list for strategies that aggregate across agents (e.g. security, complexity). May be <see langword="null" /> for topology-only flows.
    /// </param>
    /// <param name="topologyTask">Topology agent task when resolving topology acceptance; otherwise <see langword="null" />.</param>
    /// <param name="topologyResult">Topology agent result (for base confidence); otherwise <see langword="null" />.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="runId" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clock" /> or <paramref name="evaluations" /> is <see langword="null" />.</exception>
    public DecisionStrategyParameters(
        string runId,
        TimeProvider clock,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        IReadOnlyCollection<AgentTask>? tasks,
        AgentTask? topologyTask,
        AgentResult? topologyResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(evaluations);

        RunId = runId;
        Clock = clock;
        Evaluations = evaluations;
        Tasks = tasks;
        TopologyTask = topologyTask;
        TopologyResult = topologyResult;
    }

    /// <summary>Gets the run identifier.</summary>
    public string RunId { get; }

    /// <summary>Gets the clock used for decision timestamps.</summary>
    public TimeProvider Clock { get; }

    /// <summary>Gets agent evaluations for the run.</summary>
    public IReadOnlyCollection<AgentEvaluation> Evaluations { get; }

    /// <summary>Gets agent tasks when the strategy spans multiple tasks; otherwise <see langword="null" />.</summary>
    public IReadOnlyCollection<AgentTask>? Tasks { get; }

    /// <summary>Gets the topology task when required by the strategy; otherwise <see langword="null" />.</summary>
    public AgentTask? TopologyTask { get; }

    /// <summary>Gets the topology result when required by the strategy; otherwise <see langword="null" />.</summary>
    public AgentResult? TopologyResult { get; }
}

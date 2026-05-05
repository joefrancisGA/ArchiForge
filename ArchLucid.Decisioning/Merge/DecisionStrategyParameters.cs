using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Inputs shared by <see cref="IDecisionStrategy" /> implementations for a single resolve pass.
/// </summary>
public sealed class DecisionStrategyParameters
{
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

    public string RunId { get; }

    public TimeProvider Clock { get; }

    public IReadOnlyCollection<AgentEvaluation> Evaluations { get; }

    public IReadOnlyCollection<AgentTask>? Tasks { get; }

    public AgentTask? TopologyTask { get; }

    public AgentResult? TopologyResult { get; }
}

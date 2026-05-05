using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Decision Engine v2: weighted argument resolution (deterministic, v1 scoring model).
/// </summary>
public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    private readonly TimeProvider _clock;
    private readonly IDecisionStrategy _topologyAcceptance;
    private readonly IDecisionStrategy _securityControls;
    private readonly IDecisionStrategy _complexity;

    public DecisionEngineV2(TimeProvider? timeProvider = null)
    {
        _clock = timeProvider ?? TimeProvider.System;
        _topologyAcceptance = new TopologyAcceptanceDecisionStrategy();
        _securityControls = new SecurityControlsDecisionStrategy();
        _complexity = new ComplexityDecisionStrategy();
    }

    public Task<IReadOnlyList<DecisionNode>> ResolveAsync(
        string runId,
        ArchitectureRequest request,
        IReadOnlyCollection<AgentTask> tasks,
        IReadOnlyCollection<AgentResult> results,
        IReadOnlyCollection<AgentEvaluation> evaluations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(evaluations);
        cancellationToken.ThrowIfCancellationRequested();

        List<DecisionNode> decisions = [];

        AgentTask? topologyTask = tasks.FirstOrDefault(t => t.AgentType == AgentType.Topology);
        AgentResult? topologyResult = results.FirstOrDefault(r => r.AgentType == AgentType.Topology);

        if (topologyTask is null || topologyResult is null)
            return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);

        DecisionStrategyParameters topologyParameters = new(
            runId,
            _clock,
            evaluations,
            tasks: null,
            topologyTask,
            topologyResult);

        decisions.Add(_topologyAcceptance.Build(topologyParameters));

        DecisionStrategyParameters tasksParameters = new(
            runId,
            _clock,
            evaluations,
            tasks,
            topologyTask: null,
            topologyResult: null);

        decisions.Add(_securityControls.Build(tasksParameters));
        decisions.Add(_complexity.Build(tasksParameters));
        return Task.FromResult<IReadOnlyList<DecisionNode>>(decisions);
    }
}

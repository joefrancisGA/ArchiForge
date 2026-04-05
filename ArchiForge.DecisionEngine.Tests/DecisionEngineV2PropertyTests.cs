using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Services;

using FsCheck.Xunit;

namespace ArchiForge.DecisionEngine.Tests;

/// <summary>
/// Property-based checks for deterministic merge/scoring in <see cref="DecisionEngineV2"/>.
/// </summary>
[Trait("Suite", "Core")]
public sealed class DecisionEngineV2PropertyTests
{
    private readonly DecisionEngineV2 _engine = new();

    [Fact]
    public async Task When_topology_task_only_without_result_returns_empty()
    {
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T-topo",
                RunId = "RUN-P",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-P",
            new ArchitectureRequest { RequestId = "R", SystemName = "SysNameHere", Description = "Description long enough." },
            tasks,
            results: [],
            evaluations: []);

        Assert.Empty(decisions);
    }

#pragma warning disable xUnit1031 // FsCheck runs properties synchronously; DecisionEngineV2 exposes Task-only API.
    [Property(MaxTest = 60)]
    public void When_topology_pair_exists_each_node_selects_an_option(int confidenceMillis)
    {
        double confidence = (Math.Abs(confidenceMillis) % 1001) / 1000.0;

        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T-topo",
                RunId = "RUN-P2",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-P2",
                TaskId = "T-topo",
                AgentType = AgentType.Topology,
                Confidence = confidence,
                ResultId = "ar",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        IReadOnlyList<DecisionNode> decisions = _engine.ResolveAsync(
            "RUN-P2",
            new ArchitectureRequest { RequestId = "R", SystemName = "SysNameHere", Description = "Description long enough." },
            tasks,
            results,
            evaluations: []).GetAwaiter().GetResult();

        Assert.Equal(3, decisions.Count);

        foreach (DecisionNode node in decisions)
        {
            Assert.NotEmpty(node.Options);
            Assert.Contains(node.SelectedOptionId, node.Options.Select(o => o.OptionId));
        }
    }

    [Property(MaxTest = 40)]
    public void Topology_accept_rejects_scores_follow_final_score_order(int supportMillis, int opposeMillis)
    {
        double support = (Math.Abs(supportMillis) % 500) / 100.0;
        double oppose = (Math.Abs(opposeMillis) % 500) / 100.0;

        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T-topo",
                RunId = "RUN-P3",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-P3",
                TaskId = "T-topo",
                AgentType = AgentType.Topology,
                Confidence = 0.55,
                ResultId = "ar",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        List<AgentEvaluation> evaluations =
        [
            new()
            {
                RunId = "RUN-P3",
                TargetAgentTaskId = "T-topo",
                EvaluationType = "support",
                ConfidenceDelta = support,
                Rationale = "support",
            },
            new()
            {
                RunId = "RUN-P3",
                TargetAgentTaskId = "T-topo",
                EvaluationType = "oppose",
                ConfidenceDelta = -oppose,
                Rationale = "oppose",
            },
        ];

        IReadOnlyList<DecisionNode> decisions = _engine.ResolveAsync(
            "RUN-P3",
            new ArchitectureRequest { RequestId = "R", SystemName = "SysNameHere", Description = "Description long enough." },
            tasks,
            results,
            evaluations).GetAwaiter().GetResult();

        DecisionNode topology = decisions.Single(d => d.Topic == "TopologyAcceptance");
        DecisionOption accept = topology.Options.Single(o => o.Description == "Accept topology proposal");
        DecisionOption reject = topology.Options.Single(o => o.Description == "Reject topology proposal");

        bool expectAccept = accept.FinalScore >= reject.FinalScore;
        Assert.Equal(
            expectAccept ? accept.OptionId : reject.OptionId,
            topology.SelectedOptionId);
    }
#pragma warning restore xUnit1031
}

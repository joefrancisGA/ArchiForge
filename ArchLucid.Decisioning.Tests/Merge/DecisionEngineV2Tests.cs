using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;

using EvalTypes = ArchLucid.Contracts.Decisions.EvaluationTypes;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Merge;

/// <summary>
/// Tests for Decision Engine V2.
/// </summary>

[Trait("Suite", "Core")]
public sealed class DecisionEngineV2Tests
{
    private readonly DecisionEngineV2 _engine = new();

    [Fact]
    public async Task ResolveAsync_WhenSingleProposal_SelectsInclude()
    {
        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-1",
                AgentType = AgentType.Topology,
                Confidence = 0.8,
                EvidenceRefs = ["req"],
                ProposedChanges = new ManifestDeltaProposal
                {
                    SourceAgent = AgentType.Topology,
                    AddedDatastores =
                    [
                        new() { DatastoreName = "redis" }
                    ]
                }
            }
        ];

#pragma warning disable IDE0028 // Simplify collection initialization
        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-1",
            request: new ArchitectureRequest { RequestId = "REQ-1", SystemName = "S", Description = "d" },
            tasks: new List<AgentTask> { new() { TaskId = "T-1", RunId = "RUN-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed } },
            results: results,
            evaluations: []);
#pragma warning restore IDE0028 // Simplify collection initialization

        DecisionNode node = decisions.Single(d => d.Topic == "TopologyAcceptance");
        node.Options.Should().HaveCount(2);
        node.SelectedOptionId.Should().Be(node.Options.Single(o => o.Description == "Accept topology proposal").OptionId);
    }

    [Fact]
    public async Task ResolveAsync_WhenOpposed_SelectsExclude()
    {
        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-1",
                AgentType = AgentType.Topology,
                Confidence = 0.6,
                ProposedChanges = new ManifestDeltaProposal
                {
                    SourceAgent = AgentType.Topology,
                    AddedDatastores =
                    [
                        new() { DatastoreName = "redis" }
                    ]
                }
            }
        ];

        List<AgentEvaluation> evals =
        [
            new()
            {
                RunId = "RUN-1",
                TargetAgentTaskId = "T-1",
                EvaluationType = "oppose",
                ConfidenceDelta = -1.0,
                Rationale = "Oppose topology changes."
            }
        ];

#pragma warning disable IDE0028 // Simplify collection initialization
        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-1",
            request: new ArchitectureRequest { RequestId = "REQ-1", SystemName = "S", Description = "d" },
            tasks: new List<AgentTask> { new() { TaskId = "T-1", RunId = "RUN-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed } },
            results: results,
            evaluations: evals);
#pragma warning restore IDE0028 // Simplify collection initialization

        DecisionNode node = decisions.Single(d => d.Topic == "TopologyAcceptance");
        node.SelectedOptionId.Should().Be(node.Options.Single(o => o.Description == "Reject topology proposal").OptionId);
        node.OpposingEvaluationIds.Should().Contain(evals[0].EvaluationId);
    }

    /// <summary>
    ///     A single Compliance-style <see cref="EvalTypes.Oppose" /> with <c>-0.30</c> delta outweighs topology confidence
    ///     <c>0.69</c> (+ tiny Support signals), so TopologyAcceptance must select reject—validates veto behavior independent of
    ///     simulator finding mapping (<see cref="ArchLucid.Application.Decisions.FindingsBackedAgentEvaluationService" />).
    /// </summary>
    [Fact]
    public async Task ResolveAsync_when_critical_compliance_opposition_outweighs_topology_confidence_selects_reject()
    {
        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-CRIT",
                TaskId = "T-topo",
                AgentType = AgentType.Topology,
                Confidence = 0.69,
                ProposedChanges = new ManifestDeltaProposal
                {
                    SourceAgent = AgentType.Topology,
                    AddedDatastores =
                    [
                        new() { DatastoreName = "blob" }
                    ]
                }
            }
        ];

        List<AgentEvaluation> evals =
        [
            new()
            {
                RunId = "RUN-CRIT",
                TargetAgentTaskId = "T-topo",
                EvaluationType = EvalTypes.Oppose,
                ConfidenceDelta = -0.30,
                Rationale = "Public storage without private endpoint (Critical Compliance finding)."
            }
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-CRIT",
            new ArchitectureRequest { RequestId = "REQ-1", SystemName = "S", Description = "d" },
            tasks:
            [
                new()
                {
                    TaskId = "T-topo",
                    RunId = "RUN-CRIT",
                    AgentType = AgentType.Topology,
                    Status = AgentTaskStatus.Completed
                }
            ],
            results,
            evals);

        DecisionNode topologyNode = decisions.Single(d => d.Topic == "TopologyAcceptance");
        topologyNode.SelectedOptionId.Should()
            .Be(topologyNode.Options.Single(o => o.Description == "Reject topology proposal").OptionId);
    }

    [Fact]
    public async Task ResolveAsync_when_no_topology_pair_returns_empty_list()
    {
        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-EMPTY",
            new ArchitectureRequest { RequestId = "R", SystemName = "S", Description = "d" },
            tasks: new List<AgentTask>
            {
                new()
                {
                    TaskId = "T-1",
                    RunId = "RUN-EMPTY",
                    AgentType = AgentType.Compliance,
                    Status = AgentTaskStatus.Completed,
                },
            },
            results: [],
            evaluations: []);

        decisions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_when_topology_present_emits_three_topics()
    {
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T-topo",
                RunId = "RUN-MULTI",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-MULTI",
                TaskId = "T-topo",
                AgentType = AgentType.Topology,
                Confidence = 0.75,
                ResultId = "ar",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-MULTI",
            new ArchitectureRequest { RequestId = "R", SystemName = "S", Description = "d" },
            tasks,
            results,
            evaluations: []);

        decisions.Select(d => d.Topic).Should().BeEquivalentTo("TopologyAcceptance", "SecurityControlPromotion", "ComplexityDisposition");
    }

    [Fact]
    public async Task ResolveAsync_security_node_promotes_private_endpoints_when_strengthen_mentions_private()
    {
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T1",
                RunId = "RUN-SEC",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-SEC",
                TaskId = "T1",
                AgentType = AgentType.Topology,
                Confidence = 0.7,
                ResultId = "r",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        List<AgentEvaluation> evals =
        [
            new()
            {
                RunId = "RUN-SEC",
                TargetAgentTaskId = "T1",
                EvaluationType = "strengthen",
                ConfidenceDelta = 0.2,
                Rationale = "Prefer private endpoints for data planes.",
            },
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-SEC",
            new ArchitectureRequest { RequestId = "R", SystemName = "S", Description = "d" },
            tasks,
            results,
            evals);

        DecisionNode security = decisions.Single(d => d.Topic == "SecurityControlPromotion");
        security.Rationale.Should().Contain("Private Endpoints");
    }

    [Fact]
    public async Task ResolveAsync_complexity_prefers_reduce_when_caution_present()
    {
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "T1",
                RunId = "RUN-CX",
                AgentType = AgentType.Topology,
                Status = AgentTaskStatus.Completed,
            },
        ];

        List<AgentResult> results =
        [
            new()
            {
                RunId = "RUN-CX",
                TaskId = "T1",
                AgentType = AgentType.Topology,
                Confidence = 0.5,
                ResultId = "r",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        List<AgentEvaluation> evals =
        [
            new()
            {
                RunId = "RUN-CX",
                TargetAgentTaskId = "T1",
                EvaluationType = "caution",
                ConfidenceDelta = -0.5,
                Rationale = "Too many moving parts.",
            },
        ];

        IReadOnlyList<DecisionNode> decisions = await _engine.ResolveAsync(
            "RUN-CX",
            new ArchitectureRequest { RequestId = "R", SystemName = "S", Description = "d" },
            tasks,
            results,
            evals);

        DecisionNode complexity = decisions.Single(d => d.Topic == "ComplexityDisposition");
        complexity.SelectedOptionId.Should()
            .Be(complexity.Options.Single(o => o.Description.Contains("Reduce complexity", StringComparison.OrdinalIgnoreCase)).OptionId);
    }
}


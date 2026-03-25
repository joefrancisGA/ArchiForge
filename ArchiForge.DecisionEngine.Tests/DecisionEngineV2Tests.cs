using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Requests;
using ArchiForge.DecisionEngine.Services;

using FluentAssertions;

namespace ArchiForge.DecisionEngine.Tests;

public sealed class DecisionEngineV2Tests
{
    private readonly DecisionEngineV2 _engine = new();

    [Fact]
    public async Task ResolveAsync_WhenSingleProposal_SelectsInclude()
    {
        List<AgentResult> results = new()
        {
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
        };

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
        List<AgentResult> results = new()
        {
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
        };

        List<AgentEvaluation> evals = new()
        {
            new()
            {
                RunId = "RUN-1",
                TargetAgentTaskId = "T-1",
                EvaluationType = "oppose",
                ConfidenceDelta = -1.0,
                Rationale = "Oppose topology changes."
            }
        };

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
}


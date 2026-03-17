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
        var results = new List<AgentResult>
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

        var decisions = await _engine.ResolveAsync(
            "RUN-1",
            request: new ArchitectureRequest { RequestId = "REQ-1", SystemName = "S", Description = "d" },
            evidence: new AgentEvidencePackage { RunId = "RUN-1", RequestId = "REQ-1", SystemName = "S" },
            tasks: new List<AgentTask> { new() { TaskId = "T-1", RunId = "RUN-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed } },
            results: results,
            evaluations: []);

        var node = decisions.Single(d => d.Topic == "TopologyAcceptance");
        node.Options.Should().HaveCount(2);
        node.SelectedOptionId.Should().Be(node.Options.Single(o => o.Description == "Accept topology proposal").OptionId);
    }

    [Fact]
    public async Task ResolveAsync_WhenOpposed_SelectsExclude()
    {
        var results = new List<AgentResult>
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

        var evals = new List<AgentEvaluation>
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

        var decisions = await _engine.ResolveAsync(
            "RUN-1",
            request: new ArchitectureRequest { RequestId = "REQ-1", SystemName = "S", Description = "d" },
            evidence: new AgentEvidencePackage { RunId = "RUN-1", RequestId = "REQ-1", SystemName = "S" },
            tasks: new List<AgentTask> { new() { TaskId = "T-1", RunId = "RUN-1", AgentType = AgentType.Topology, Status = AgentTaskStatus.Completed } },
            results: results,
            evaluations: evals);

        var node = decisions.Single(d => d.Topic == "TopologyAcceptance");
        node.SelectedOptionId.Should().Be(node.Options.Single(o => o.Description == "Reject topology proposal").OptionId);
        node.OpposingEvaluationIds.Should().Contain(evals[0].EvaluationId);
    }
}


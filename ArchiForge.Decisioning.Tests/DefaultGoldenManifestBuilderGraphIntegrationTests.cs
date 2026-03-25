using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Rules;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class DefaultGoldenManifestBuilderGraphIntegrationTests
{
    [Fact]
    public async Task Build_includes_topology_resource_labels_from_graph_snapshot()
    {
        Guid runId = Guid.NewGuid();
        Guid ctxId = Guid.NewGuid();
        GraphSnapshot graph = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            Nodes =
            [
                new GraphNode
                {
                    NodeId = "t1",
                    NodeType = "TopologyResource",
                    Label = "hub-vnet",
                    Category = "network",
                    Properties = new()
                }
            ]
        };

        FindingsSnapshot findings = new FindingsSnapshot
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = ctxId,
            GraphSnapshotId = graph.GraphSnapshotId,
            Findings = []
        };

        DecisionTrace trace = new DecisionTrace { DecisionTraceId = Guid.NewGuid(), RunId = runId };
        DecisionRuleSet ruleSet = await new InMemoryDecisionRuleProvider().GetRuleSetAsync(CancellationToken.None);

        GoldenManifest manifest = new DefaultGoldenManifestBuilder().Build(
            runId,
            ctxId,
            graph,
            findings,
            trace,
            ruleSet);

        manifest.Topology.Resources.Should().Contain("hub-vnet");
    }
}

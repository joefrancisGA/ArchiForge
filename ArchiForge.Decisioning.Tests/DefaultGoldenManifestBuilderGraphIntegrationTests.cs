using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Decisioning.Manifest.Builders;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Rules;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Integration tests: Default Golden Manifest Builder Graph (HTTP host, database, or cross-component).
/// </summary>
[Trait("Category", "Unit")]
public sealed class DefaultGoldenManifestBuilderGraphIntegrationTests
{
    [Fact]
    public async Task Build_includes_topology_resource_labels_from_graph_snapshot()
    {
        Guid runId = Guid.NewGuid();
        Guid ctxId = Guid.NewGuid();
        GraphSnapshot graph = new()
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

        FindingsSnapshot findings = new()
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = ctxId,
            GraphSnapshotId = graph.GraphSnapshotId,
            Findings = []
        };

        DecisionTrace trace = DecisionTrace.FromRuleAudit(new RuleAuditTracePayload
        {
            DecisionTraceId = Guid.NewGuid(),
            RunId = runId
        });
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

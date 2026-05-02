using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Graph Snapshot Extensions.
/// </summary>
[Trait("Category", "Unit")]
public sealed class GraphSnapshotExtensionsTests
{
    [Fact]
    public void GetIncomingSources_returns_nodes_with_edges_to_target()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "a", NodeType = "SecurityBaseline", Label = "sec", Properties = new() },
                new GraphNode { NodeId = "t", NodeType = "TopologyResource", Label = "net", Properties = new() }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "a",
                    ToNodeId = "t",
                    EdgeType = "PROTECTS",
                    Label = "protects"
                }
            ]
        };

        IReadOnlyList<GraphNode> sources = graph.GetIncomingSources("t", "PROTECTS");

        sources.Should().ContainSingle();
        sources[0].NodeId.Should().Be("a");
    }

    [Fact]
    public void GetOutgoingTargets_when_min_weight_excludes_low_weight_edges()
    {
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "a", NodeType = "SecurityBaseline", Label = "sec", Properties = new() },
                new GraphNode { NodeId = "t", NodeType = "TopologyResource", Label = "net", Properties = new() }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "a",
                    ToNodeId = "t",
                    EdgeType = "PROTECTS",
                    Label = "protects",
                    Weight = 0.3d
                }
            ]
        };

        graph.GetOutgoingTargets("a", "PROTECTS", GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
            .Should()
            .BeEmpty();

        graph.GetOutgoingTargets("a", "PROTECTS", 0).Should().ContainSingle();
    }
}

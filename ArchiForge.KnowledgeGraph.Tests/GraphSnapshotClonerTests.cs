using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Services;

using FluentAssertions;

namespace ArchiForge.KnowledgeGraph.Tests;

[Trait("Category", "Unit")]
public sealed class GraphSnapshotClonerTests
{
    [Fact]
    public void CloneForNewRun_UsesNewIdsAndPreservesTopology()
    {
        Guid oldGraphId = Guid.NewGuid();
        Guid oldContextId = Guid.NewGuid();
        Guid oldRunId = Guid.NewGuid();
        GraphEdge edge = new()
        {
            EdgeId = "edge-old",
            FromNodeId = "a",
            ToNodeId = "b",
            EdgeType = "contains",
            Weight = 0.75,
            Properties = new Dictionary<string, string> { ["k"] = "v" }
        };
        GraphSnapshot source = new()
        {
            GraphSnapshotId = oldGraphId,
            ContextSnapshotId = oldContextId,
            RunId = oldRunId,
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            Nodes =
            [
                new GraphNode
                {
                    NodeId = "a",
                    NodeType = "TopologyResource",
                    Label = "net",
                    Properties = new Dictionary<string, string> { ["p"] = "q" }
                }
            ],
            Edges = [edge],
            Warnings = ["w"]
        };

        ContextSnapshot newContext = new()
        {
            SnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ProjectId = "proj",
            CreatedUtc = DateTime.UtcNow
        };

        Guid newRunId = Guid.NewGuid();
        GraphSnapshot clone = GraphSnapshotCloner.CloneForNewRun(source, newContext, newRunId);

        clone.GraphSnapshotId.Should().NotBe(oldGraphId);
        clone.ContextSnapshotId.Should().Be(newContext.SnapshotId);
        clone.RunId.Should().Be(newRunId);
        clone.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        clone.Nodes.Should().HaveCount(1);
        clone.Nodes[0].NodeId.Should().Be("a");
        clone.Nodes[0].Properties["p"].Should().Be("q");
        clone.Warnings.Should().Equal("w");
        clone.Edges.Should().HaveCount(1);
        clone.Edges[0].EdgeId.Should().NotBe("edge-old");
        clone.Edges[0].FromNodeId.Should().Be("a");
        clone.Edges[0].ToNodeId.Should().Be("b");
        clone.Edges[0].Weight.Should().Be(0.75);
        clone.Edges[0].Properties["k"].Should().Be("v");
    }
}

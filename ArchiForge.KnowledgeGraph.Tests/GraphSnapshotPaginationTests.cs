using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.KnowledgeGraph.Tests;

public sealed class GraphSnapshotPaginationTests
{
    [Fact]
    public void CreatePage_FirstPage_InducesEdgesOnlyWithinPage()
    {
        GraphSnapshot snapshot = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "a", NodeType = "t", Label = "A" },
                new GraphNode { NodeId = "b", NodeType = "t", Label = "B" },
                new GraphNode { NodeId = "c", NodeType = "t", Label = "C" }
            ],
            Edges =
            [
                new GraphEdge { EdgeId = "e1", FromNodeId = "a", ToNodeId = "b", EdgeType = "calls" },
                new GraphEdge { EdgeId = "e2", FromNodeId = "b", ToNodeId = "c", EdgeType = "calls" }
            ]
        };

        GraphSnapshotNodesPage page = GraphSnapshotPagination.CreatePage(snapshot, page: 1, pageSize: 2);

        page.TotalNodes.Should().Be(3);
        page.Nodes.Should().HaveCount(2);
        page.Nodes[0].NodeId.Should().Be("a");
        page.Nodes[1].NodeId.Should().Be("b");
        page.Edges.Should().ContainSingle();
        page.Edges[0].FromNodeId.Should().Be("a");
        page.Edges[0].ToNodeId.Should().Be("b");
        page.HasMore.Should().BeTrue();
    }

    [Fact]
    public void CreatePage_LastPage_HasMoreFalse()
    {
        GraphSnapshot snapshot = new()
        {
            Nodes = [new GraphNode { NodeId = "x", NodeType = "t", Label = "X" }]
        };

        GraphSnapshotNodesPage page = GraphSnapshotPagination.CreatePage(snapshot, page: 1, pageSize: 50);

        page.HasMore.Should().BeFalse();
        page.TotalNodes.Should().Be(1);
    }
}

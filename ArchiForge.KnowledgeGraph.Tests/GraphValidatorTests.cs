using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Services;

using FluentAssertions;

namespace ArchiForge.KnowledgeGraph.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GraphValidatorTests
{
    private readonly GraphValidator _sut = new();

    [Fact]
    public void Validate_EmptySnapshot_DoesNotThrow()
    {
        GraphSnapshot snapshot = BuildSnapshot();

        Action act = () => _sut.Validate(snapshot);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullSnapshot_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_NodeMissingNodeId_ThrowsInvalidOperationException()
    {
        GraphSnapshot snapshot = BuildSnapshot(new GraphNode
        {
            NodeId = "   ",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = "node-a"
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NodeId*");
    }

    [Fact]
    public void Validate_NodeMissingNodeType_ThrowsInvalidOperationException()
    {
        GraphSnapshot snapshot = BuildSnapshot(new GraphNode
        {
            NodeId = "n1",
            NodeType = "",
            Label = "node-a"
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().Throw<InvalidOperationException>().WithMessage("*NodeType*");
    }

    [Fact]
    public void Validate_EdgeMissingEdgeType_ThrowsInvalidOperationException()
    {
        GraphNode node = ValidNode("n1");
        GraphSnapshot snapshot = BuildSnapshot(node);
        snapshot.Edges.Add(new GraphEdge
        {
            EdgeId = "e1",
            FromNodeId = "n1",
            ToNodeId = "n1",
            EdgeType = ""
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().Throw<InvalidOperationException>().WithMessage("*EdgeType*");
    }

    [Fact]
    public void Validate_EdgeFromNodeNotInSnapshot_ThrowsInvalidOperationException()
    {
        GraphNode node = ValidNode("n1");
        GraphSnapshot snapshot = BuildSnapshot(node);
        snapshot.Edges.Add(new GraphEdge
        {
            EdgeId = "e1",
            FromNodeId = "missing-node",
            ToNodeId = "n1",
            EdgeType = GraphEdgeTypes.Contains
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().Throw<InvalidOperationException>().WithMessage("*missing-node*");
    }

    [Fact]
    public void Validate_EdgeToNodeNotInSnapshot_ThrowsInvalidOperationException()
    {
        GraphNode node = ValidNode("n1");
        GraphSnapshot snapshot = BuildSnapshot(node);
        snapshot.Edges.Add(new GraphEdge
        {
            EdgeId = "e1",
            FromNodeId = "n1",
            ToNodeId = "ghost",
            EdgeType = GraphEdgeTypes.Contains
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ghost*");
    }

    [Fact]
    public void Validate_ValidNodesAndEdges_DoesNotThrow()
    {
        GraphNode a = ValidNode("a");
        GraphNode b = ValidNode("b");
        GraphSnapshot snapshot = BuildSnapshot(a, b);
        snapshot.Edges.Add(new GraphEdge
        {
            EdgeId = "e1",
            FromNodeId = "a",
            ToNodeId = "b",
            EdgeType = GraphEdgeTypes.Contains
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().NotThrow();
    }

    // NodeId comparison is case-insensitive per HashSet constructor.
    [Fact]
    public void Validate_EdgeWithDifferentCaseNodeId_DoesNotThrow()
    {
        GraphNode node = ValidNode("NodeA");
        GraphSnapshot snapshot = BuildSnapshot(node);
        snapshot.Edges.Add(new GraphEdge
        {
            EdgeId = "e1",
            FromNodeId = "nodea",
            ToNodeId = "NODEA",
            EdgeType = GraphEdgeTypes.Contains
        });

        Action act = () => _sut.Validate(snapshot);

        act.Should().NotThrow();
    }

    private static GraphSnapshot BuildSnapshot(params GraphNode[] nodes)
    {
        GraphSnapshot snapshot = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };

        foreach (GraphNode n in nodes)
        {
            snapshot.Nodes.Add(n);
        }

        return snapshot;
    }

    private static GraphNode ValidNode(string nodeId) =>
        new()
        {
            NodeId = nodeId,
            NodeType = GraphNodeTypes.TopologyResource,
            Label = nodeId
        };
}

using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Inference;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary>
///     Tests for Default Graph Edge Inferer.
/// </summary>
[Trait("Category", "Unit")]
public sealed class DefaultGraphEdgeInfererTests
{
    private readonly DefaultGraphEdgeInferer _sut = new();

    [Fact]
    public void InferEdges_NullSnapshot_ThrowsArgumentNullException()
    {
        Action act = () => _sut.InferEdges(null!, []);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InferEdges_NullNodes_ThrowsArgumentNullException()
    {
        Action act = () => _sut.InferEdges(BuildSnapshot(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InferEdges_EmptySnapshot_ReturnsEmptyList()
    {
        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(BuildSnapshot(), []);

        edges.Should().BeEmpty();
    }

    [Fact]
    public void InferEdges_ContextNodeOnly_HasNoEdges()
    {
        GraphNode contextNode = new()
        {
            NodeId = $"context-{Guid.NewGuid():N}", NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(BuildSnapshot(), [contextNode]);

        // Context node is excluded from CONTAINS edges (only non-context nodes get one).
        edges.Should().BeEmpty();
    }

    [Fact]
    public void InferEdges_TopologyNode_GetsContainsEdgeFromContext()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };
        GraphNode topology = new()
        {
            NodeId = "res-1",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = "vnet",
            Category = GraphTopologyCategories.Network
        };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, topology]);

        edges.Should().Contain(e =>
            e.FromNodeId == contextNodeId &&
            e.ToNodeId == "res-1" &&
            e.EdgeType == GraphEdgeTypes.Contains);
    }

    [Fact]
    public void InferEdges_SecurityNode_ProtectsTopologyResource()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };
        GraphNode security = new() { NodeId = "sec-1", NodeType = GraphNodeTypes.SecurityBaseline, Label = "baseline" };
        GraphNode resource = new() { NodeId = "res-1", NodeType = GraphNodeTypes.TopologyResource, Label = "vnet" };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, security, resource]);

        edges.Should().Contain(e =>
            e.FromNodeId == "sec-1" &&
            e.ToNodeId == "res-1" &&
            e.EdgeType == GraphEdgeTypes.Protects);
    }

    [Fact]
    public void InferEdges_PolicyNode_AppliesToTopologyResource()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };
        GraphNode policy = new() { NodeId = "pol-1", NodeType = GraphNodeTypes.PolicyControl, Label = "SOC2" };
        GraphNode resource = new() { NodeId = "res-1", NodeType = GraphNodeTypes.TopologyResource, Label = "compute" };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, policy, resource]);

        edges.Should().Contain(e =>
            e.FromNodeId == "pol-1" &&
            e.ToNodeId == "res-1" &&
            e.EdgeType == GraphEdgeTypes.AppliesTo);
    }

    [Fact]
    public void InferEdges_RequirementMentioningNetwork_RelatesToVnet()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };
        GraphNode req = new()
        {
            NodeId = "req-1",
            NodeType = GraphNodeTypes.Requirement,
            Label = "Network isolation required",
            Properties = new Dictionary<string, string> { ["text"] = "network isolation required" }
        };
        GraphNode vnet = new()
        {
            NodeId = "res-vnet",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = "vnet",
            Category = GraphTopologyCategories.Network
        };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, req, vnet]);

        edges.Should().Contain(e =>
            e.FromNodeId == "req-1" &&
            e.ToNodeId == "res-vnet" &&
            e.EdgeType == GraphEdgeTypes.RelatesTo);
    }

    [Fact]
    public void InferEdges_NetworkContainsSubnet_ContainsResourceEdge()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };
        GraphNode network = new()
        {
            NodeId = "net-1",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = "vnet",
            Category = GraphTopologyCategories.Network
        };
        GraphNode subnet = new()
        {
            NodeId = "sub-1", NodeType = GraphNodeTypes.TopologyResource, Label = "subnet-private"
        };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, network, subnet]);

        edges.Should().Contain(e =>
            e.FromNodeId == "net-1" &&
            e.ToNodeId == "sub-1" &&
            e.EdgeType == GraphEdgeTypes.ContainsResource);
    }

    [Fact]
    public void InferEdges_DuplicateEdgesAreDeduped()
    {
        ContextSnapshot snapshot = BuildSnapshot();
        string contextNodeId = $"context-{snapshot.SnapshotId:N}";
        GraphNode contextNode = new()
        {
            NodeId = contextNodeId, NodeType = GraphNodeTypes.ContextSnapshot, Label = "ctx"
        };

        // Two topology nodes; inferrer runs containment twice for network+subnet.
        GraphNode network = new()
        {
            NodeId = "net-1",
            NodeType = GraphNodeTypes.TopologyResource,
            Label = "vnet",
            Category = GraphTopologyCategories.Network
        };
        GraphNode subnet1 = new() { NodeId = "sub-1", NodeType = GraphNodeTypes.TopologyResource, Label = "subnet-a" };

        IReadOnlyList<GraphEdge> edges = _sut.InferEdges(snapshot, [contextNode, network, subnet1]);

        // ContainsResource net-1 → sub-1 should appear exactly once.
        int count = edges.Count(e => e is
            { FromNodeId: "net-1", ToNodeId: "sub-1", EdgeType: GraphEdgeTypes.ContainsResource });
        count.Should().Be(1);
    }

    private static ContextSnapshot BuildSnapshot()
    {
        return new ContextSnapshot { SnapshotId = Guid.NewGuid(), RunId = Guid.NewGuid(), ProjectId = "proj-test" };
    }
}

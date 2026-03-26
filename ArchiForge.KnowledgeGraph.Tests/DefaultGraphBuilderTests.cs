using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Builders;
using ArchiForge.KnowledgeGraph.Inference;
using ArchiForge.KnowledgeGraph.Mapping;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

using Moq;

namespace ArchiForge.KnowledgeGraph.Tests;

[Trait("Category", "Unit")]
public sealed class DefaultGraphBuilderTests
{
    private static ContextSnapshot BasicSnapshot(params CanonicalObject[] objects) => new()
    {
        SnapshotId = new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
        RunId = Guid.NewGuid(),
        ProjectId = "test-project",
        CanonicalObjects = [.. objects]
    };

    [Fact]
    public async Task BuildAsync_NullSnapshot_Throws()
    {
        DefaultGraphBuilder sut = BuildSut(new Mock<IGraphNodeFactory>(), new Mock<IGraphEdgeInferer>());

        Func<Task> act = () => sut.BuildAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BuildAsync_EmptySnapshot_ReturnsContextNodeOnly()
    {
        Mock<IGraphEdgeInferer> edgeInferer = new(MockBehavior.Strict);
        edgeInferer
            .Setup(e => e.InferEdges(It.IsAny<ContextSnapshot>(), It.IsAny<IReadOnlyList<GraphNode>>()))
            .Returns([]);

        DefaultGraphBuilder sut = BuildSut(new Mock<IGraphNodeFactory>(MockBehavior.Strict), edgeInferer);

        ContextSnapshot snapshot = BasicSnapshot();

        GraphBuildResult result = await sut.BuildAsync(snapshot, CancellationToken.None);

        result.Nodes.Should().ContainSingle(n =>
            n.NodeType == GraphNodeTypes.ContextSnapshot &&
            n.NodeId == $"context-{snapshot.SnapshotId:N}");

        result.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ContextNodeId_MatchesSnapshotId()
    {
        Guid snapshotId = Guid.NewGuid();

        Mock<IGraphEdgeInferer> edgeInferer = new();
        edgeInferer
            .Setup(e => e.InferEdges(It.IsAny<ContextSnapshot>(), It.IsAny<IReadOnlyList<GraphNode>>()))
            .Returns([]);

        DefaultGraphBuilder sut = BuildSut(new Mock<IGraphNodeFactory>(), edgeInferer);

        ContextSnapshot snapshot = new()
        {
            SnapshotId = snapshotId,
            RunId = Guid.NewGuid(),
            ProjectId = "proj-x"
        };

        GraphBuildResult result = await sut.BuildAsync(snapshot, CancellationToken.None);

        result.Nodes.Should().Contain(n => n.NodeId == $"context-{snapshotId:N}");
    }

    [Fact]
    public async Task BuildAsync_WithCanonicalObjects_CallsNodeFactoryForEach()
    {
        CanonicalObject obj1 = new()
        {
            ObjectId = Guid.NewGuid().ToString(),
            ObjectType = GraphNodeTypes.TopologyResource,
            Name = "Storage Account"
        };
        CanonicalObject obj2 = new()
        {
            ObjectId = Guid.NewGuid().ToString(),
            ObjectType = GraphNodeTypes.SecurityBaseline,
            Name = "Encryption Policy"
        };

        GraphNode node1 = new() { NodeId = $"obj-{obj1.ObjectId}", NodeType = obj1.ObjectType, Label = obj1.Name };
        GraphNode node2 = new() { NodeId = $"obj-{obj2.ObjectId}", NodeType = obj2.ObjectType, Label = obj2.Name };

        Mock<IGraphNodeFactory> nodeFactory = new(MockBehavior.Strict);
        nodeFactory.Setup(f => f.CreateNode(obj1)).Returns(node1);
        nodeFactory.Setup(f => f.CreateNode(obj2)).Returns(node2);

        Mock<IGraphEdgeInferer> edgeInferer = new();
        edgeInferer
            .Setup(e => e.InferEdges(It.IsAny<ContextSnapshot>(), It.IsAny<IReadOnlyList<GraphNode>>()))
            .Returns([]);

        DefaultGraphBuilder sut = BuildSut(nodeFactory, edgeInferer);

        GraphBuildResult result = await sut.BuildAsync(BasicSnapshot(obj1, obj2), CancellationToken.None);

        // context node + 2 canonical object nodes
        result.Nodes.Should().HaveCount(3);
        result.Nodes.Should().Contain(n => n.NodeId == node1.NodeId);
        result.Nodes.Should().Contain(n => n.NodeId == node2.NodeId);

        nodeFactory.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_InferredEdgesAddedToResult()
    {
        Mock<IGraphEdgeInferer> edgeInferer = new(MockBehavior.Strict);
        GraphEdge inferredEdge = new()
        {
            EdgeId = Guid.NewGuid().ToString("N"),
            FromNodeId = "a",
            ToNodeId = "b",
            EdgeType = GraphEdgeTypes.Contains,
            Label = "contains"
        };
        edgeInferer
            .Setup(e => e.InferEdges(It.IsAny<ContextSnapshot>(), It.IsAny<IReadOnlyList<GraphNode>>()))
            .Returns([inferredEdge]);

        DefaultGraphBuilder sut = BuildSut(new Mock<IGraphNodeFactory>(MockBehavior.Strict), edgeInferer);

        GraphBuildResult result = await sut.BuildAsync(BasicSnapshot(), CancellationToken.None);

        result.Edges.Should().ContainSingle().Which.Should().Be(inferredEdge);
    }

    [Fact]
    public async Task BuildAsync_ContextNodeProperties_ContainSnapshotAndRunId()
    {
        Mock<IGraphEdgeInferer> edgeInferer = new();
        edgeInferer
            .Setup(e => e.InferEdges(It.IsAny<ContextSnapshot>(), It.IsAny<IReadOnlyList<GraphNode>>()))
            .Returns([]);

        DefaultGraphBuilder sut = BuildSut(new Mock<IGraphNodeFactory>(), edgeInferer);

        ContextSnapshot snapshot = new()
        {
            SnapshotId = new Guid("bbbbbbbb-0000-0000-0000-000000000099"),
            RunId = new Guid("cccccccc-0000-0000-0000-000000000099"),
            ProjectId = "proj-abc"
        };

        GraphBuildResult result = await sut.BuildAsync(snapshot, CancellationToken.None);

        GraphNode contextNode = result.Nodes.Single(n => n.NodeType == GraphNodeTypes.ContextSnapshot);
        contextNode.Properties["snapshotId"].Should().Be(snapshot.SnapshotId.ToString());
        contextNode.Properties["runId"].Should().Be(snapshot.RunId.ToString());
        contextNode.Properties["projectId"].Should().Be(snapshot.ProjectId);
    }

    private static DefaultGraphBuilder BuildSut(
        Mock<IGraphNodeFactory> nodeFactory,
        Mock<IGraphEdgeInferer> edgeInferer)
        => new(nodeFactory.Object, edgeInferer.Object);
}

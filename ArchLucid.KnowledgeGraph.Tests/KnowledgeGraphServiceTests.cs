using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.KnowledgeGraph.Services;

using FluentAssertions;

using Moq;

namespace ArchLucid.KnowledgeGraph.Tests;

/// <summary>
///     Tests for Knowledge Graph Service.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class KnowledgeGraphServiceTests
{
    private readonly Mock<IGraphBuilder> _graphBuilderMock = new(MockBehavior.Strict);
    private readonly Mock<IGraphValidator> _graphValidatorMock = new(MockBehavior.Strict);
    private readonly KnowledgeGraphService _sut;

    public KnowledgeGraphServiceTests()
    {
        _sut = new KnowledgeGraphService(_graphBuilderMock.Object, _graphValidatorMock.Object);
    }

    [Fact]
    public async Task BuildSnapshotAsync_NullSnapshot_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.BuildSnapshotAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task BuildSnapshotAsync_CallsBuilderThenValidator()
    {
        ContextSnapshot contextSnapshot = BuildContextSnapshot();
        GraphBuildResult buildResult = new();
        buildResult.Nodes.Add(new GraphNode
        {
            NodeId = "n1", NodeType = GraphNodeTypes.TopologyResource, Label = "n1"
        });

        _graphBuilderMock
            .Setup(b => b.BuildAsync(contextSnapshot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildResult);

        _graphValidatorMock
            .Setup(v => v.Validate(It.IsAny<GraphSnapshot>()));

        GraphSnapshot snapshot = await _sut.BuildSnapshotAsync(contextSnapshot, CancellationToken.None);

        _graphBuilderMock.Verify(b => b.BuildAsync(contextSnapshot, It.IsAny<CancellationToken>()), Times.Once);
        _graphValidatorMock.Verify(v => v.Validate(It.IsAny<GraphSnapshot>()), Times.Once);
        snapshot.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildSnapshotAsync_SetsIdsFromContextSnapshot()
    {
        ContextSnapshot contextSnapshot = BuildContextSnapshot();
        GraphBuildResult buildResult = new();

        _graphBuilderMock
            .Setup(b => b.BuildAsync(contextSnapshot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildResult);

        _graphValidatorMock
            .Setup(v => v.Validate(It.IsAny<GraphSnapshot>()));

        GraphSnapshot snapshot = await _sut.BuildSnapshotAsync(contextSnapshot, CancellationToken.None);

        snapshot.ContextSnapshotId.Should().Be(contextSnapshot.SnapshotId);
        snapshot.RunId.Should().Be(contextSnapshot.RunId);
        snapshot.GraphSnapshotId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BuildSnapshotAsync_CopiesNodesAndEdgesFromBuildResult()
    {
        ContextSnapshot contextSnapshot = BuildContextSnapshot();
        GraphBuildResult buildResult = new();
        buildResult.Nodes.Add(new GraphNode { NodeId = "x", NodeType = GraphNodeTypes.Requirement, Label = "req" });
        buildResult.Edges.Add(new GraphEdge
        {
            EdgeId = "e1", FromNodeId = "x", ToNodeId = "x", EdgeType = GraphEdgeTypes.Contains
        });

        _graphBuilderMock
            .Setup(b => b.BuildAsync(contextSnapshot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildResult);

        _graphValidatorMock
            .Setup(v => v.Validate(It.IsAny<GraphSnapshot>()));

        GraphSnapshot snapshot = await _sut.BuildSnapshotAsync(contextSnapshot, CancellationToken.None);

        snapshot.Nodes.Should().HaveCount(1).And.Contain(n => n.NodeId == "x");
        snapshot.Edges.Should().HaveCount(1).And.Contain(e => e.EdgeId == "e1");
    }

    [Fact]
    public async Task BuildSnapshotAsync_ValidatorThrows_PropagatesException()
    {
        ContextSnapshot contextSnapshot = BuildContextSnapshot();
        GraphBuildResult buildResult = new();

        _graphBuilderMock
            .Setup(b => b.BuildAsync(contextSnapshot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildResult);

        _graphValidatorMock
            .Setup(v => v.Validate(It.IsAny<GraphSnapshot>()))
            .Throws(new InvalidOperationException("Graph node NodeId is required."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.BuildSnapshotAsync(contextSnapshot, CancellationToken.None));
    }

    private static ContextSnapshot BuildContextSnapshot()
    {
        return new ContextSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ProjectId = "proj-test",
            CreatedUtc = DateTime.UtcNow
        };
    }
}

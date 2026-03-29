using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Repositories;
using ArchiForge.Persistence.Serialization;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class GraphSnapshotStorageMapperTests
{
    [Fact]
    public void ToSnapshot_WhenRowIsNull_ThrowsArgumentNullException()
    {
        Action act = () => GraphSnapshotStorageMapper.ToSnapshot(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToSnapshot_WhenJsonValid_ReturnsGraphSnapshot()
    {
        Guid graphId = Guid.NewGuid();
        Guid contextId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        DateTime created = DateTime.UtcNow;

        string nodesJson = JsonEntitySerializer.Serialize(new List<GraphNode>());
        string edgesJson = JsonEntitySerializer.Serialize(new List<GraphEdge>());
        string warningsJson = JsonEntitySerializer.Serialize(new List<string> { "w1" });

        GraphSnapshotStorageRow row = new()
        {
            GraphSnapshotId = graphId,
            ContextSnapshotId = contextId,
            RunId = runId,
            CreatedUtc = created,
            NodesJson = nodesJson,
            EdgesJson = edgesJson,
            WarningsJson = warningsJson,
        };

        GraphSnapshot snapshot = GraphSnapshotStorageMapper.ToSnapshot(row);

        snapshot.GraphSnapshotId.Should().Be(graphId);
        snapshot.ContextSnapshotId.Should().Be(contextId);
        snapshot.RunId.Should().Be(runId);
        snapshot.CreatedUtc.Should().Be(created);
        snapshot.Nodes.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
        snapshot.Warnings.Should().Equal("w1");
    }

    [Fact]
    public void ToSnapshot_WhenOverridesProvided_UsesOverridesInsteadOfJson()
    {
        Guid graphId = Guid.NewGuid();
        GraphSnapshotStorageRow row = new()
        {
            GraphSnapshotId = graphId,
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            NodesJson = "{invalid nodes",
            EdgesJson = "{invalid edges",
            WarningsJson = "{invalid warnings",
        };

        List<GraphNode> nodes = [new GraphNode { NodeId = "n1", NodeType = "T", Label = "L" }];
        List<GraphEdge> edges =
        [
            new GraphEdge
            {
                EdgeId = "e1",
                FromNodeId = "a",
                ToNodeId = "b",
                EdgeType = "X",
                Weight = 1d,
            },
        ];

        List<string> warnings = ["from-relational"];

        GraphSnapshot snapshot = GraphSnapshotStorageMapper.ToSnapshot(row, nodes, edges, warnings);

        snapshot.Nodes.Should().ContainSingle(n => n.NodeId == "n1");
        snapshot.Edges.Should().ContainSingle(e => e.EdgeId == "e1");
        snapshot.Warnings.Should().Equal("from-relational");
    }

    [Fact]
    public void ToSnapshot_WhenNodesJsonInvalid_WrapsDeserializationFailure()
    {
        GraphSnapshotStorageRow row = new()
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            NodesJson = "{not valid",
            EdgesJson = JsonEntitySerializer.Serialize(new List<GraphEdge>()),
            WarningsJson = JsonEntitySerializer.Serialize(new List<string>()),
        };

        Action act = () => GraphSnapshotStorageMapper.ToSnapshot(row);

        InvalidOperationException ex = act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{row.GraphSnapshotId}*")
            .Which;

        ex.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.InnerException!.InnerException.Should().BeOfType<System.Text.Json.JsonException>();
    }
}

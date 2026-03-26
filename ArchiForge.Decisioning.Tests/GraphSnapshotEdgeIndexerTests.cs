using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Repositories;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class GraphSnapshotEdgeIndexerTests
{
    [Fact]
    public void BuildRows_MapsEachEdge()
    {
        Guid graphId = Guid.NewGuid();
        GraphSnapshot snapshot = new()
        {
            GraphSnapshotId = graphId,
            ContextSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "a",
                    ToNodeId = "b",
                    EdgeType = "CONTAINS",
                    Weight = 2d
                },
                new GraphEdge
                {
                    EdgeId = "e2",
                    FromNodeId = "b",
                    ToNodeId = "c",
                    EdgeType = "RELATES_TO",
                    Weight = 1d
                }
            ]
        };

        IReadOnlyList<GraphSnapshotEdgeRow> rows = GraphSnapshotEdgeIndexer.BuildRows(snapshot);

        rows.Should().HaveCount(2);
        rows[0].Should().Be(new GraphSnapshotEdgeRow(graphId, "e1", "a", "b", "CONTAINS", 2d));
        rows[1].Should().Be(new GraphSnapshotEdgeRow(graphId, "e2", "b", "c", "RELATES_TO", 1d));
    }
}

using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// Builds denormalized edge rows for <c>dbo.GraphSnapshotEdges</c> from a <see cref="GraphSnapshot"/>.
/// </summary>
public static class GraphSnapshotEdgeIndexer
{
    public static IReadOnlyList<GraphSnapshotEdgeRow> BuildRows(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<GraphSnapshotEdgeRow> rows = [];
        foreach (GraphEdge edge in snapshot.Edges)
        
            rows.Add(new GraphSnapshotEdgeRow(
                snapshot.GraphSnapshotId,
                edge.EdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.EdgeType,
                edge.Weight));
        

        return rows;
    }
}

/// <summary>One row for indexed edge queries (mirrors <see cref="GraphEdge"/> + snapshot scope).</summary>
public sealed record GraphSnapshotEdgeRow(
    Guid GraphSnapshotId,
    string EdgeId,
    string FromNodeId,
    string ToNodeId,
    string EdgeType,
    double Weight);

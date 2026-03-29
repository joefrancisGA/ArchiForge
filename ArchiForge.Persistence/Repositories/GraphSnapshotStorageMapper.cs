using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Serialization;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// Maps persisted graph snapshot rows to domain <see cref="GraphSnapshot"/> (shared by SQL repository and unit tests).
/// </summary>
public static class GraphSnapshotStorageMapper
{
    /// <summary>
    /// Deserializes JSON columns into a <see cref="GraphSnapshot"/>; wraps deserialization failures in a single message.
    /// </summary>
    public static GraphSnapshot ToSnapshot(GraphSnapshotStorageRow row)
    {
        return ToSnapshot(row, nodesOverride: null, edgesOverride: null, warningsOverride: null);
    }

    /// <summary>
    /// Builds a <see cref="GraphSnapshot"/> from the header row. When an override list is non-null, that collection
    /// is taken from the override instead of deserializing the matching JSON column (relational-first read path).
    /// </summary>
    public static GraphSnapshot ToSnapshot(
        GraphSnapshotStorageRow row,
        IReadOnlyList<GraphNode>? nodesOverride,
        IReadOnlyList<GraphEdge>? edgesOverride,
        IReadOnlyList<string>? warningsOverride)
    {
        ArgumentNullException.ThrowIfNull(row);

        try
        {
            List<GraphNode> nodes = nodesOverride is null
                ? JsonEntitySerializer.Deserialize<List<GraphNode>>(row.NodesJson)
                : nodesOverride.ToList();

            List<GraphEdge> edges = edgesOverride is null
                ? JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson)
                : edgesOverride.ToList();

            List<string> warnings = warningsOverride is null
                ? JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson)
                : warningsOverride.ToList();

            return new GraphSnapshot
            {
                GraphSnapshotId = row.GraphSnapshotId,
                ContextSnapshotId = row.ContextSnapshotId,
                RunId = row.RunId,
                CreatedUtc = row.CreatedUtc,
                Nodes = nodes,
                Edges = edges,
                Warnings = warnings,
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize GraphSnapshot '{row.GraphSnapshotId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.",
                ex);
        }
    }
}

using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     Maps persisted graph snapshot rows to domain <see cref="GraphSnapshot" /> (shared by SQL repository and unit
///     tests).
/// </summary>
public static class GraphSnapshotStorageMapper
{
    /// <summary>Builds a snapshot by deserializing JSON columns (unit tests and callers without relational overrides).</summary>
    public static GraphSnapshot ToSnapshot(GraphSnapshotStorageRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        try
        {
            return new GraphSnapshot
            {
                GraphSnapshotId = row.GraphSnapshotId,
                ContextSnapshotId = row.ContextSnapshotId,
                RunId = row.RunId,
                CreatedUtc = row.CreatedUtc,
                Nodes = JsonEntitySerializer.Deserialize<List<GraphNode>>(row.NodesJson),
                Edges = JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson),
                Warnings = JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson)
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

    /// <summary>
    ///     Builds a <see cref="GraphSnapshot" /> from the header row. When an override list is non-null, that collection
    ///     is taken from relational tables; when null, the collection is empty (JSON columns are not read — relational read
    ///     path).
    /// </summary>
    public static GraphSnapshot ToSnapshot(
        GraphSnapshotStorageRow row,
        IReadOnlyList<GraphNode>? nodesOverride,
        IReadOnlyList<GraphEdge>? edgesOverride,
        IReadOnlyList<string>? warningsOverride)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new GraphSnapshot
        {
            GraphSnapshotId = row.GraphSnapshotId,
            ContextSnapshotId = row.ContextSnapshotId,
            RunId = row.RunId,
            CreatedUtc = row.CreatedUtc,
            Nodes = nodesOverride is not null ? nodesOverride.ToList() : [],
            Edges = edgesOverride is not null ? edgesOverride.ToList() : [],
            Warnings = warningsOverride is not null ? warningsOverride.ToList() : []
        };
    }
}

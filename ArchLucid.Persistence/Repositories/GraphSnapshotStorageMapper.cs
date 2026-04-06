using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.RelationalRead;
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
        return ToSnapshot(row, nodesOverride: null, edgesOverride: null, warningsOverride: null, fallbackPolicy: null);
    }

    /// <summary>
    /// Builds a <see cref="GraphSnapshot"/> from the header row. When an override list is non-null, that collection
    /// is taken from the override instead of deserializing the matching JSON column (relational-first read path).
    /// When override is null, <paramref name="fallbackPolicy"/> governs whether JSON columns are read.
    /// </summary>
    public static GraphSnapshot ToSnapshot(
        GraphSnapshotStorageRow row,
        IReadOnlyList<GraphNode>? nodesOverride,
        IReadOnlyList<GraphEdge>? edgesOverride,
        IReadOnlyList<string>? warningsOverride,
        JsonFallbackPolicy? fallbackPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(row);

        string entityId = row.GraphSnapshotId.ToString();

        try
        {
            List<GraphNode> nodes = ResolveOverrideOrFallback(
                nodesOverride,
                () => JsonEntitySerializer.Deserialize<List<GraphNode>>(row.NodesJson),
                fallbackPolicy,
                "GraphSnapshot.Nodes",
                entityId);

            List<GraphEdge> edges = ResolveOverrideOrFallback(
                edgesOverride,
                () => JsonEntitySerializer.Deserialize<List<GraphEdge>>(row.EdgesJson),
                fallbackPolicy,
                "GraphSnapshot.Edges",
                entityId);

            List<string> warnings = ResolveOverrideOrFallback(
                warningsOverride,
                () => JsonEntitySerializer.Deserialize<List<string>>(row.WarningsJson),
                fallbackPolicy,
                "GraphSnapshot.Warnings",
                entityId);

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

    private static List<T> ResolveOverrideOrFallback<T>(
        IReadOnlyList<T>? relationalOverride,
        Func<List<T>> deserializeJson,
        JsonFallbackPolicy? policy,
        string sliceName,
        string entityId)
    {
        if (relationalOverride is not null)
            return relationalOverride.ToList();

        if (policy is null || policy.EvaluateFallback(0, sliceName, "GraphSnapshot", entityId))
            return deserializeJson();

        return [];
    }
}

namespace ArchLucid.KnowledgeGraph.Models;

public static class GraphSnapshotExtensions
{
    public static IReadOnlyList<GraphNode> GetNodesByType(
        this GraphSnapshot snapshot,
        string nodeType)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Nodes
            .Where(x => string.Equals(x.NodeType, nodeType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<GraphEdge> GetEdgesByType(
        this GraphSnapshot snapshot,
        string edgeType)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Edges
            .Where(x => string.Equals(x.EdgeType, edgeType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<GraphNode> GetOutgoingTargets(
        this GraphSnapshot snapshot,
        string fromNodeId,
        string edgeType,
        double minWeightInclusive = 0)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        HashSet<string> targetIds = snapshot.Edges
            .Where(x =>
                string.Equals(x.FromNodeId, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.EdgeType, edgeType, StringComparison.OrdinalIgnoreCase) &&
                x.Weight >= minWeightInclusive)
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Nodes
            .Where(x => targetIds.Contains(x.NodeId))
            .ToList();
    }

    /// <summary>
    ///     Nodes that have an incoming <paramref name="edgeType" /> edge to <paramref name="toNodeId" />.
    /// </summary>
    public static IReadOnlyList<GraphNode> GetIncomingSources(
        this GraphSnapshot snapshot,
        string toNodeId,
        string edgeType)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        HashSet<string> sourceIds = snapshot.Edges
            .Where(x =>
                string.Equals(x.ToNodeId, toNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.EdgeType, edgeType, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.FromNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Nodes
            .Where(x => sourceIds.Contains(x.NodeId))
            .ToList();
    }
}

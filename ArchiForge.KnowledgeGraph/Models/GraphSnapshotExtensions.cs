namespace ArchiForge.KnowledgeGraph.Models;

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
        string edgeType)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var targetIds = snapshot.Edges
            .Where(x =>
                string.Equals(x.FromNodeId, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.EdgeType, edgeType, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return snapshot.Nodes
            .Where(x => targetIds.Contains(x.NodeId))
            .ToList();
    }
}

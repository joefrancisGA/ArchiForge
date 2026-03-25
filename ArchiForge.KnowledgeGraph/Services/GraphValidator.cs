using ArchiForge.KnowledgeGraph.Interfaces;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Services;

public class GraphValidator : IGraphValidator
{
    public void Validate(GraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        HashSet<string> nodeIds = new(snapshot.Nodes.Select(x => x.NodeId), StringComparer.OrdinalIgnoreCase);

        foreach (GraphNode node in snapshot.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
                throw new InvalidOperationException("Graph node NodeId is required.");

            if (string.IsNullOrWhiteSpace(node.NodeType))
                throw new InvalidOperationException($"Graph node '{node.NodeId}' is missing NodeType.");
        }

        foreach (GraphEdge edge in snapshot.Edges)
        {
            if (string.IsNullOrWhiteSpace(edge.EdgeType))
                throw new InvalidOperationException("Graph edge EdgeType is required.");

            if (!nodeIds.Contains(edge.FromNodeId))
                throw new InvalidOperationException($"Graph edge source node '{edge.FromNodeId}' does not exist.");

            if (!nodeIds.Contains(edge.ToNodeId))
                throw new InvalidOperationException($"Graph edge target node '{edge.ToNodeId}' does not exist.");
        }
    }
}

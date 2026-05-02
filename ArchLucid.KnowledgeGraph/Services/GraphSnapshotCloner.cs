using ArchLucid.ContextIngestion.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.KnowledgeGraph.Services;

/// <summary>
///     Clones a persisted <see cref="GraphSnapshot" /> for a new run when
///     <see cref="GraphSnapshotCanonicalFingerprint" /> indicates an equivalent canonical context.
/// </summary>
public static class GraphSnapshotCloner
{
    /// <summary>
    ///     Produces a new snapshot with fresh <see cref="GraphSnapshot.GraphSnapshotId" /> and edge ids,
    ///     bound to <paramref name="newContext" /> and <paramref name="runId" />.
    /// </summary>
    public static GraphSnapshot CloneForNewRun(GraphSnapshot source, ContextSnapshot newContext, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(newContext);

        return new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ContextSnapshotId = newContext.SnapshotId,
            RunId = runId,
            CreatedUtc = DateTime.UtcNow,
            Nodes = [.. source.Nodes.Select(CloneNode)],
            Edges = [.. source.Edges.Select(CloneEdge)],
            Warnings = [.. source.Warnings]
        };
    }

    private static GraphNode CloneNode(GraphNode node)
    {
        return new GraphNode
        {
            NodeId = node.NodeId,
            NodeType = node.NodeType,
            Label = node.Label,
            Category = node.Category,
            SourceType = node.SourceType,
            SourceId = node.SourceId,
            Properties = new Dictionary<string, string>(node.Properties, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static GraphEdge CloneEdge(GraphEdge edge)
    {
        return new GraphEdge
        {
            EdgeId = Guid.NewGuid().ToString("N"),
            FromNodeId = edge.FromNodeId,
            ToNodeId = edge.ToNodeId,
            EdgeType = edge.EdgeType,
            Label = edge.Label,
            Weight = edge.Weight,
            InferenceSource = edge.InferenceSource,
            Properties = new Dictionary<string, string>(edge.Properties, StringComparer.OrdinalIgnoreCase)
        };
    }
}

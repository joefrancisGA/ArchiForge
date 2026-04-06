namespace ArchiForge.KnowledgeGraph.Models;

public class GraphSnapshot
{
    public Guid GraphSnapshotId { get; set; }
    public Guid ContextSnapshotId { get; set; }
    public Guid RunId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<GraphNode> Nodes { get; set; } = [];
    public List<GraphEdge> Edges { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}


namespace ArchiForge.KnowledgeGraph.Models;

public class GraphEdge
{
    public string EdgeId { get; set; } = null!;

    public string FromNodeId { get; set; } = null!;

    public string ToNodeId { get; set; } = null!;

    public string EdgeType { get; set; } = null!;
}


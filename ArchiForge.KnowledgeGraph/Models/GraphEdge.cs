namespace ArchiForge.KnowledgeGraph.Models;

public class GraphEdge
{
    public string EdgeId { get; set; } = default!;

    public string FromNodeId { get; set; } = default!;

    public string ToNodeId { get; set; } = default!;

    public string EdgeType { get; set; } = default!;

    public string? Label { get; set; }

    public Dictionary<string, string> Properties { get; set; } = new();
}

namespace ArchiForge.KnowledgeGraph.Models;

public class GraphNode
{
    public string NodeId { get; set; } = null!;

    public string NodeType { get; set; } = null!;

    public string Label { get; set; } = null!;

    public Dictionary<string, string> Properties { get; set; } = new();
}


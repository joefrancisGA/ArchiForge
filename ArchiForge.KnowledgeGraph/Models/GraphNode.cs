namespace ArchiForge.KnowledgeGraph.Models;

public class GraphNode
{
    public string NodeId { get; set; } = null!;

    public string NodeType { get; set; } = null!;

    public string Label { get; set; } = null!;

    public string? Category { get; set; }

    public string? SourceType { get; set; }

    public string? SourceId { get; set; }

    public Dictionary<string, string> Properties { get; set; } = new();
}

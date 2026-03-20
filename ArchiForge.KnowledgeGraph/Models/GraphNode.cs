namespace ArchiForge.KnowledgeGraph.Models;

public class GraphNode
{
    public string NodeId { get; set; } = default!;

    public string NodeType { get; set; } = default!;

    public string Label { get; set; } = default!;

    public string? Category { get; set; }

    public string? SourceType { get; set; }

    public string? SourceId { get; set; }

    public Dictionary<string, string> Properties { get; set; } = new();
}

namespace ArchiForge.ArtifactSynthesis.Models;

public class DiagramAst
{
    public string Title { get; set; } = null!;
    public List<DiagramNode> Nodes { get; set; } = new();
    public List<DiagramEdge> Edges { get; set; } = new();
}

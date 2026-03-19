using System.Text;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Renderers;

public class MermaidDiagramRenderer : IDiagramRenderer
{
    public string Format => "mermaid";

    public string Render(DiagramAst ast)
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");

        foreach (var node in ast.Nodes)
        {
            var safeLabel = node.Label.Replace("\"", "'", StringComparison.Ordinal);
            sb.AppendLine($"    {node.NodeId}[\"{safeLabel}\"]");
        }

        foreach (var edge in ast.Edges)
        {
            var safeLabel = edge.Label.Replace("\"", "'", StringComparison.Ordinal);
            sb.AppendLine($"    {edge.FromNodeId} -->|\"{safeLabel}\"| {edge.ToNodeId}");
        }

        return sb.ToString();
    }
}

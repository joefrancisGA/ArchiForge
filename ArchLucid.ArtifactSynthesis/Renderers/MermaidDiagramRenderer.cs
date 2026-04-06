using System.Text;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Renderers;

public class MermaidDiagramRenderer : IDiagramRenderer
{
    public string Format => "mermaid";

    public string Render(DiagramAst ast)
    {
        StringBuilder sb = new();
        sb.AppendLine("flowchart TD");

        foreach (DiagramNode node in ast.Nodes)
        {
            string safeLabel = node.Label.Replace("\"", "'", StringComparison.Ordinal);
            sb.AppendLine($"    {node.NodeId}[\"{safeLabel}\"]");
        }

        foreach (DiagramEdge edge in ast.Edges)
        {
            string safeLabel = edge.Label.Replace("\"", "'", StringComparison.Ordinal);
            sb.AppendLine($"    {edge.FromNodeId} -->|\"{safeLabel}\"| {edge.ToNodeId}");
        }

        return sb.ToString();
    }
}

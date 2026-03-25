using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class MermaidDiagramArtifactGenerator(IDiagramRenderer renderer) : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.MermaidDiagram;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        DiagramAst ast = new()
        {
            Title = manifest.Metadata.Name
        };

        ast.Nodes.Add(new DiagramNode
        {
            NodeId = "manifest",
            Label = "Golden Manifest",
            NodeType = "Manifest"
        });

        foreach (ResolvedArchitectureDecision decision in manifest.Decisions)
        {
            string nodeId = $"decision_{decision.DecisionId}";
            ast.Nodes.Add(new DiagramNode
            {
                NodeId = nodeId,
                Label = decision.Title,
                NodeType = decision.Category
            });

            ast.Edges.Add(new DiagramEdge
            {
                FromNodeId = "manifest",
                ToNodeId = nodeId,
                Label = "decision"
            });
        }

        string content = renderer.Render(ast);

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.MermaidDiagram,
            Name = "architecture.mmd",
            Format = renderer.Format,
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content),
            Metadata = new Dictionary<string, string>
            {
                ["title"] = ast.Title
            }
        });
    }
}

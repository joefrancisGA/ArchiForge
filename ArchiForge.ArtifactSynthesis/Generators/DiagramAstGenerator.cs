using System.Text.Json;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Services;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Generators;

public class DiagramAstGenerator : IArtifactGenerator
{
    public string ArtifactType => global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.DiagramAst;

    public Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct)
    {
        _ = ct;
        var ast = new DiagramAst
        {
            Title = manifest.Metadata.Name
        };

        ast.Nodes.Add(new DiagramNode
        {
            NodeId = "manifest",
            Label = "Golden Manifest",
            NodeType = "Manifest"
        });

        foreach (var decision in manifest.Decisions)
        {
            var nodeId = $"decision-{decision.DecisionId}";
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
                Label = "contains"
            });
        }

        for (var i = 0; i < manifest.UnresolvedIssues.Items.Count; i++)
        {
            var issue = manifest.UnresolvedIssues.Items[i];
            var nodeId = $"issue-{i}";
            ast.Nodes.Add(new DiagramNode
            {
                NodeId = nodeId,
                Label = issue.Title,
                NodeType = "Issue"
            });

            ast.Edges.Add(new DiagramEdge
            {
                FromNodeId = "manifest",
                ToNodeId = nodeId,
                Label = "flags"
            });
        }

        var content = JsonSerializer.Serialize(ast, SynthesisJsonOptions.WriteIndented);

        return Task.FromResult(new SynthesizedArtifact
        {
            ArtifactId = Guid.NewGuid(),
            RunId = manifest.RunId,
            ManifestId = manifest.ManifestId,
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = global::ArchiForge.ArtifactSynthesis.Models.ArtifactType.DiagramAst,
            Name = "diagram-ast.json",
            Format = "json",
            Content = content,
            ContentHash = ArtifactHashing.ComputeHash(content)
        });
    }
}

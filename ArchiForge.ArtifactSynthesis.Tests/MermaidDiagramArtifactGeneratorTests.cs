using FluentAssertions;

using ArchiForge.ArtifactSynthesis.Generators;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;

using Moq;

namespace ArchiForge.ArtifactSynthesis.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class MermaidDiagramArtifactGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_builds_ast_from_manifest_and_uses_renderer()
    {
        Mock<IDiagramRenderer> renderer = new();
        renderer.Setup(x => x.Format).Returns("mermaid");
        renderer.Setup(x => x.Render(It.IsAny<ArchiForge.ArtifactSynthesis.Models.DiagramAst>()))
            .Returns("rendered-mermaid");

        GoldenManifest manifest = new()
        {
            RunId = Guid.NewGuid(),
            ManifestId = Guid.NewGuid(),
            Metadata = new ManifestMetadata { Name = "Sys" },
            Decisions =
            [
                new ResolvedArchitectureDecision
                {
                    DecisionId = "dec-1",
                    Category = "Security",
                    Title = "Use TLS",
                    SelectedOption = "TLS1.3",
                    Rationale = "Strong crypto",
                },
            ],
        };

        MermaidDiagramArtifactGenerator sut = new(renderer.Object);

        Models.SynthesizedArtifact artifact = await sut.GenerateAsync(manifest, CancellationToken.None);

        artifact.ArtifactType.Should().Be(Models.ArtifactType.MermaidDiagram);
        artifact.Content.Should().Be("rendered-mermaid");
        artifact.Metadata.Should().ContainKey("title").WhoseValue.Should().Be("Sys");
        renderer.Verify(
            x => x.Render(It.Is<ArchiForge.ArtifactSynthesis.Models.DiagramAst>(a => a.Nodes.Count >= 2)),
            Times.Once);
    }
}

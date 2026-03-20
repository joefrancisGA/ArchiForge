using ArchiForge.ArtifactSynthesis.Generators;
using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Renderers;
using ArchiForge.ArtifactSynthesis.Services;

namespace ArchiForge.Coordinator.Tests;

internal static class TestArtifactSynthesisFactory
{
    public static IArtifactSynthesisService Create()
    {
        var renderer = new MermaidDiagramRenderer();
        IEnumerable<IArtifactGenerator> generators =
        [
            new ReferenceArchitectureMarkdownGenerator(),
            new DiagramAstGenerator(),
            new MermaidDiagramArtifactGenerator(renderer),
            new InventoryArtifactGenerator(),
            new CostSummaryArtifactGenerator()
        ];
        return new ArtifactSynthesisService(
            generators,
            new ArtifactBundleValidator());
    }
}

using ArchLucid.ArtifactSynthesis.Generators;
using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Renderers;
using ArchLucid.ArtifactSynthesis.Services;

using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLucid.Application.Tests.MigratedCoordinator;

internal static class TestArtifactSynthesisFactory
{
    public static IArtifactSynthesisService Create()
    {
        MermaidDiagramRenderer renderer = new();
        IEnumerable<IArtifactGenerator> generators =
        [
            new ReferenceArchitectureMarkdownGenerator(),
            new ArchitectureNarrativeArtifactGenerator(),
            new ComplianceMatrixArtifactGenerator(),
            new CoverageSummaryArtifactGenerator(),
            new DiagramAstGenerator(),
            new MermaidDiagramArtifactGenerator(renderer),
            new InventoryArtifactGenerator(),
            new CostSummaryArtifactGenerator(),
            new UnresolvedIssuesArtifactGenerator()
        ];
        return new ArtifactSynthesisService(
            generators,
            new ArtifactBundleValidator(),
            NullLogger<ArtifactSynthesisService>.Instance);
    }
}

using ArchLucid.AgentRuntime.Explanation;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Demo;
using ArchLucid.Persistence.Models;
using ArchLucid.Provenance;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Host.Composition.Tests.Demo;

/// <summary>
///     Unit-tests for <see cref="DemoReadModelClient" />: confirms composition when a committed demo run is resolved
///     and that a missing aggregate explanation always degrades to <see langword="null" /> so the controller can return
///     404.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class DemoReadModelClientTests
{
    [Fact]
    public async Task GetLatestCommittedDemoExplainAsync_returns_payload_when_resolver_returns_committed_run()
    {
        Guid manifestId = Guid.NewGuid();
        RunRecord baseline = new()
        {
            RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = manifestId,
            CurrentManifestVersion = ContosoRetailDemoIdentifiers.ManifestBaseline,
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver
            .Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        Mock<IRunExplanationSummaryService> explainSvc = new();
        RunExplanationSummary summary = BuildSummary();
        explainSvc
            .Setup(s => s.GetSummaryAsync(It.IsAny<ScopeContext>(), baseline.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        Mock<IProvenanceQueryService> provenance = new();
        GraphViewModel graph = new()
        {
            Nodes = [new GraphNodeVm { Id = "n1", Label = "manifest", Type = "Manifest" }], Edges = []
        };
        provenance
            .Setup(p => p.GetFullGraphAsync(It.IsAny<ScopeContext>(), baseline.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(graph);

        DemoReadModelClient sut = BuildSut(seedResolver, explainSvc, provenance);

        DemoExplainResponse? response = await sut.GetLatestCommittedDemoExplainAsync();

        response.Should().NotBeNull();
        response.RunId.Should().Be(baseline.RunId.ToString("N"));
        response.ManifestVersion.Should().Be(ContosoRetailDemoIdentifiers.ManifestBaseline);
        response.IsDemoData.Should().BeTrue();
        response.RunExplanation.Should().BeSameAs(summary);
        response.ProvenanceGraph.Should().BeSameAs(graph);
    }

    [Fact]
    public async Task GetLatestCommittedDemoExplainAsync_returns_null_when_no_committed_demo_run_exists()
    {
        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver
            .Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        DemoReadModelClient sut = BuildSut(seedResolver, new Mock<IRunExplanationSummaryService>(),
            new Mock<IProvenanceQueryService>());

        DemoExplainResponse? response = await sut.GetLatestCommittedDemoExplainAsync();

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestCommittedDemoExplainAsync_returns_null_when_explanation_summary_missing()
    {
        RunRecord baseline = new()
        {
            RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver
            .Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        Mock<IRunExplanationSummaryService> explainSvc = new();
        explainSvc
            .Setup(s => s.GetSummaryAsync(It.IsAny<ScopeContext>(), baseline.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunExplanationSummary?)null);

        DemoReadModelClient sut = BuildSut(seedResolver, explainSvc, new Mock<IProvenanceQueryService>());

        DemoExplainResponse? response = await sut.GetLatestCommittedDemoExplainAsync();

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestCommittedDemoExplainAsync_substitutes_empty_graph_when_provenance_is_null()
    {
        RunRecord baseline = new()
        {
            RunId = ContosoRetailDemoIdentifiers.AuthorityRunBaselineId,
            ArchitectureRequestId = ContosoRetailDemoIdentifiers.RequestContoso,
            GoldenManifestId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IDemoSeedRunResolver> seedResolver = new();
        seedResolver
            .Setup(s => s.ResolveLatestCommittedDemoRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        Mock<IRunExplanationSummaryService> explainSvc = new();
        explainSvc
            .Setup(s => s.GetSummaryAsync(It.IsAny<ScopeContext>(), baseline.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSummary());

        Mock<IProvenanceQueryService> provenance = new();
        provenance
            .Setup(p => p.GetFullGraphAsync(It.IsAny<ScopeContext>(), baseline.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GraphViewModel?)null);

        DemoReadModelClient sut = BuildSut(seedResolver, explainSvc, provenance);

        DemoExplainResponse? response = await sut.GetLatestCommittedDemoExplainAsync();

        response.Should().NotBeNull();
        response.ProvenanceGraph.Should().NotBeNull();
        response.ProvenanceGraph.IsEmpty.Should().BeTrue();
    }

    private static DemoReadModelClient BuildSut(
        Mock<IDemoSeedRunResolver> seedResolver,
        Mock<IRunExplanationSummaryService> explainSvc,
        Mock<IProvenanceQueryService> provenance)
    {
        return new DemoReadModelClient(
            seedResolver.Object,
            explainSvc.Object,
            provenance.Object,
            TimeProvider.System,
            NullLogger<DemoReadModelClient>.Instance);
    }

    private static RunExplanationSummary BuildSummary()
    {
        return new RunExplanationSummary
        {
            Explanation = new ExplanationResult { Summary = "Summary" },
            ThemeSummaries = ["Theme A"],
            OverallAssessment = "Assessment",
            RiskPosture = "Moderate"
        };
    }
}

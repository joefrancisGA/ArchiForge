using ArchiForge.Decisioning.Analysis;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

public sealed class PolicyCoverageFindingEngineTests
{
    private readonly Mock<IGraphCoverageAnalyzer> _analyzer = new(MockBehavior.Strict);

    [Fact]
    public async Task AnalyzeAsync_NoPolicyNodes_EmitsSingleWarningFinding()
    {
        PolicyCoverageResult result = new()
        {
            PolicyNodeCount = 0,
            PolicyApplicabilityEdgeCount = 0,
            UncoveredResources = []
        };
        _analyzer.Setup(a => a.AnalyzePolicy(It.IsAny<GraphSnapshot>())).Returns(result);

        PolicyCoverageFindingEngine sut = new(_analyzer.Object);
        GraphSnapshot graph = new();

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(graph, CancellationToken.None);

        findings.Should().ContainSingle();
        findings[0].FindingType.Should().Be("PolicyCoverageFinding");
        findings[0].Severity.Should().Be(FindingSeverity.Warning);
        findings[0].Title.Should().Contain("No policy");
        PolicyCoverageFindingPayload? payload = findings[0].Payload as PolicyCoverageFindingPayload;
        payload.Should().NotBeNull();
        payload.PolicyNodeCount.Should().Be(0);
    }

    [Fact]
    public async Task AnalyzeAsync_FullCoverage_ReturnsEmptyList()
    {
        PolicyCoverageResult result = new()
        {
            PolicyNodeCount = 2,
            PolicyApplicabilityEdgeCount = 5,
            UncoveredResources = []
        };
        _analyzer.Setup(a => a.AnalyzePolicy(It.IsAny<GraphSnapshot>())).Returns(result);

        PolicyCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UncoveredResources_EmitsFindingWithPayload()
    {
        PolicyCoverageResult result = new()
        {
            PolicyNodeCount = 1,
            PolicyApplicabilityEdgeCount = 0,
            UncoveredResources = ["storage-1", "vm-2"]
        };
        _analyzer.Setup(a => a.AnalyzePolicy(It.IsAny<GraphSnapshot>())).Returns(result);

        PolicyCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().ContainSingle();
        PolicyCoverageFindingPayload? payload = findings[0].Payload as PolicyCoverageFindingPayload;
        payload.Should().NotBeNull();
        payload.UncoveredResources.Should().Equal("storage-1", "vm-2");
        payload.PolicyNodeCount.Should().Be(1);
    }

    [Fact]
    public void EngineType_And_Category_AreStable()
    {
        PolicyCoverageFindingEngine sut = new(_analyzer.Object);

        sut.EngineType.Should().Be("policy-coverage");
        sut.Category.Should().Be("Policy");
    }
}

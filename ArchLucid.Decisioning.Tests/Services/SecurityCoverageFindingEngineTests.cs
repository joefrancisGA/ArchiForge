using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using Moq;

namespace ArchLucid.Decisioning.Tests.Services;

[Trait("Category", "Unit")]
public sealed class SecurityCoverageFindingEngineTests
{
    private readonly Mock<IGraphCoverageAnalyzer> _analyzer = new(MockBehavior.Strict);

    [Fact]
    public async Task AnalyzeAsync_UnprotectedResourceCountZero_ReturnsNoFindings()
    {
        SecurityCoverageResult result = new()
        {
            SecurityNodeCount = 2,
            ProtectedResourceCount = 2,
            UnprotectedResourceCount = 0,
            UnprotectedResources = []
        };
        _analyzer.Setup(a => a.AnalyzeSecurity(It.IsAny<GraphSnapshot>())).Returns(result);

        SecurityCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UnprotectedResources_ProducesOneFindingWithExpectedFields()
    {
        SecurityCoverageResult result = new()
        {
            SecurityNodeCount = 3,
            ProtectedResourceCount = 1,
            UnprotectedResourceCount = 2,
            UnprotectedResources = ["res-a", "res-b"]
        };
        _analyzer.Setup(a => a.AnalyzeSecurity(It.IsAny<GraphSnapshot>())).Returns(result);

        SecurityCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().ContainSingle();
        Finding finding = findings[0];
        finding.FindingType.Should().Be("SecurityCoverageFinding");
        finding.Category.Should().Be("Security");
        finding.EngineType.Should().Be("security-coverage");
        finding.PayloadType.Should().Be(nameof(SecurityCoverageFindingPayload));
        finding.Payload.Should().BeOfType<SecurityCoverageFindingPayload>();

        SecurityCoverageFindingPayload? payload = finding.Payload as SecurityCoverageFindingPayload;
        payload.Should().NotBeNull();
        payload.SecurityNodeCount.Should().Be(3);
        payload.ProtectedResourceCount.Should().Be(1);
        payload.UnprotectedResourceCount.Should().Be(2);
        payload.UnprotectedResources.Should().Equal("res-a", "res-b");

        finding.Trace.GraphNodeIdsExamined.Should().Equal("res-a", "res-b");
        finding.Trace.RulesApplied.Should().Contain("security-coverage-protection");
        finding.Trace.Notes.Should().Contain(n => n.Contains("Security nodes: 3", StringComparison.Ordinal));
        finding.Trace.Notes.Should().Contain(n => n.Contains("Protected: 1", StringComparison.Ordinal) && n.Contains("Unprotected: 2", StringComparison.Ordinal));
        finding.Trace.AlternativePathsConsidered.Should().NotBeEmpty();
    }

    [Fact]
    public void EngineType_And_Category_AreStable()
    {
        SecurityCoverageFindingEngine sut = new(_analyzer.Object);

        sut.EngineType.Should().Be("security-coverage");
        sut.Category.Should().Be("Security");
    }
}

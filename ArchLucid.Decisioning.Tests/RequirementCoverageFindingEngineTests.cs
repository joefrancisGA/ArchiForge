using ArchLucid.Decisioning.Analysis;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Requirement Coverage Finding Engine.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RequirementCoverageFindingEngineTests
{
    private readonly Mock<IGraphCoverageAnalyzer> _analyzer = new(MockBehavior.Strict);

    [Fact]
    public async Task AnalyzeAsync_AllRequirementsRelated_ReturnsEmptyList()
    {
        RequirementCoverageResult result = new()
        {
            RequirementNodeCount = 3,
            RelatedRequirementCount = 3,
            UnrelatedRequirementCount = 0,
            UncoveredRequirements = []
        };
        _analyzer.Setup(a => a.AnalyzeRequirements(It.IsAny<GraphSnapshot>())).Returns(result);

        RequirementCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UnrelatedRequirements_EmitsFinding()
    {
        RequirementCoverageResult result = new()
        {
            RequirementNodeCount = 2,
            RelatedRequirementCount = 1,
            UnrelatedRequirementCount = 1,
            UncoveredRequirements = ["REQ-99"]
        };
        _analyzer.Setup(a => a.AnalyzeRequirements(It.IsAny<GraphSnapshot>())).Returns(result);

        RequirementCoverageFindingEngine sut = new(_analyzer.Object);

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(new GraphSnapshot(), CancellationToken.None);

        findings.Should().ContainSingle();
        findings[0].FindingType.Should().Be("RequirementCoverageFinding");
        RequirementCoverageFindingPayload? payload = findings[0].Payload as RequirementCoverageFindingPayload;
        payload.Should().NotBeNull();
        payload.UncoveredRequirements.Should().Contain("REQ-99");
        payload.UncoveredRequirementCount.Should().Be(1);
        findings[0].Trace.DecisionsTaken.Should().NotBeEmpty();
        findings[0].Trace.GraphNodeIdsExamined.Should().Contain("REQ-99");
    }

    [Fact]
    public void EngineType_And_Category_AreStable()
    {
        RequirementCoverageFindingEngine sut = new(_analyzer.Object);

        sut.EngineType.Should().Be("requirement-coverage");
        sut.Category.Should().Be("Requirement");
    }
}

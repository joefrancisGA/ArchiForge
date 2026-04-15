using ArchLucid.Decisioning.Compliance.Evaluators;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Decisioning.Compliance.Models;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Services;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Compliance Finding Engine.
/// </summary>

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ComplianceFindingEngineTests
{
    [Fact]
    public async Task AnalyzeAsync_MapsViolationSeverities_ToFindingSeverity()
    {
        ComplianceRulePack pack = new()
        {
            RulePackId = "test-pack",
            Name = "Test",
            Version = "1",
            RulePackHash = "h",
            SourcePath = "inline",
            Rules = []
        };

        ComplianceViolation critical = new()
        {
            RuleId = "r1",
            ControlId = "c1",
            ControlName = "ctrl",
            AppliesToCategory = "network",
            Severity = "Critical",
            Description = "d1",
            AffectedNodeIds = ["n1"],
            AffectedResources = ["a"]
        };

        ComplianceViolation unknown = new()
        {
            RuleId = "r2",
            ControlId = "c2",
            ControlName = "ctrl2",
            AppliesToCategory = "compute",
            Severity = "weird",
            Description = "d2",
            AffectedNodeIds = [],
            AffectedResources = []
        };

        Mock<IComplianceRulePackProvider> provider = new();
        provider.Setup(p => p.GetRulePackAsync(It.IsAny<CancellationToken>())).ReturnsAsync(pack);

        Mock<IComplianceRulePackValidator> validator = new();
        validator.Setup(v => v.Validate(It.IsAny<ComplianceRulePack>()));

        Mock<IComplianceEvaluator> evaluator = new();
        evaluator
            .Setup(e => e.Evaluate(It.IsAny<GraphSnapshot>(), pack))
            .Returns(
                new ComplianceEvaluationResult
                {
                    Violations = [critical, unknown]
                });

        ComplianceFindingEngine sut = new(provider.Object, validator.Object, evaluator.Object);
        GraphSnapshot graph = new() { Nodes = [], Edges = [] };

        IReadOnlyList<Finding> findings = await sut.AnalyzeAsync(graph, CancellationToken.None);

        findings.Should().HaveCount(2);
        findings[0].Severity.Should().Be(FindingSeverity.Critical);
        findings[0].Payload.Should().BeOfType<ComplianceFindingPayload>();
        findings[0].Trace.DecisionsTaken.Should().NotBeEmpty();
        findings[0].Trace.Notes.Should().Contain("Rule pack: test-pack v1");
        findings[0].Trace.AlternativePathsConsidered.Should().ContainSingle()
            .Which.Should().Be(ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote);
        findings[1].Severity.Should().Be(FindingSeverity.Info);
        findings[1].Trace.DecisionsTaken.Should().NotBeEmpty();
        findings[1].Trace.AlternativePathsConsidered.Should().ContainSingle()
            .Which.Should().Be(ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote);
    }
}

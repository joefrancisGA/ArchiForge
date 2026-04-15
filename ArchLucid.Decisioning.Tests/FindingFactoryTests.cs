using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Finding Factory.
/// </summary>
[Trait("Category", "Unit")]
public sealed class FindingFactoryTests
{
    [Fact]
    public void CreateRequirementFinding_SetsSchemaVersionAndPayloadType()
    {
        Finding f = FindingFactory.CreateRequirementFinding(
            "requirement", "t", "r", "N", "text", true);

        f.FindingSchemaVersion.Should().Be(FindingsSchema.CurrentFindingVersion);
        f.PayloadType.Should().Be(nameof(RequirementFindingPayload));
        f.Category.Should().Be("Requirement");
    }

    [Fact]
    public void CreateTopologyGapFinding_PopulatesExplainabilityTrace()
    {
        Finding f = FindingFactory.CreateTopologyGapFinding(
            "topology-gap-engine",
            "Gap title",
            "Rationale",
            gapCode: "missing-edge",
            description: "No path between subnets",
            impact: "high",
            relatedNodeIds: ["n1", "n2"]);

        f.Trace.GraphNodeIdsExamined.Should().Equal("n1", "n2");
        f.Trace.RulesApplied.Should().Contain("topology-gap-missing-edge");
        f.Trace.DecisionsTaken.Should().ContainSingle()
            .Which.Should().Be("Detected topology gap: No path between subnets");
        f.Trace.AlternativePathsConsidered.Should().ContainSingle()
            .Which.Should().Be(ExplainabilityTraceMarkers.RuleBasedDeterministicSinglePathNote);
    }
}

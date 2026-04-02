using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

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
}

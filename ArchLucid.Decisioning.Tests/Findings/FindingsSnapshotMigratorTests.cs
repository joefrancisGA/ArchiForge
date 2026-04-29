using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Models;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Findings;

[Trait("Category", "Unit")]
public sealed class FindingsSnapshotMigratorTests
{
    [Fact]
    public void FindingsSchema_CurrentVersions_AreIntConstants()
    {
        FindingsSchema.CurrentFindingVersion.Should().Be(2);
        FindingsSchema.CurrentSnapshotVersion.Should().Be(2);
    }

    [Fact]
    public void Apply_FindingAtCurrentVersion_IsNotModified()
    {
        Finding finding = new()
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingType = "RequirementFinding",
            FindingId = "stable",
            EngineType = "e",
            Title = "t",
            Rationale = "r",
            Category = "",
            PayloadType = null
        };
        FindingsSnapshot snapshot = new()
        {
            SchemaVersion = 0,
            Findings = [finding]
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        finding.Category.Should().BeEmpty();
        finding.PayloadType.Should().BeNull();
        finding.FindingSchemaVersion.Should().Be(FindingsSchema.CurrentFindingVersion);
        snapshot.SchemaVersion.Should().Be(FindingsSchema.CurrentSnapshotVersion);
    }

    [Theory]
    [InlineData("RequirementFinding", "Requirement", "RequirementFindingPayload")]
    [InlineData("TopologyGap", "Topology", "TopologyGapFindingPayload")]
    [InlineData("SecurityControlFinding", "Security", "SecurityControlFindingPayload")]
    [InlineData("CostConstraintFinding", "Cost", "CostConstraintFindingPayload")]
    [InlineData("UnknownFinding", "General", null)]
    public void Apply_InfersCategoryAndPayloadType_FromFindingType(
        string findingType,
        string expectedCategory,
        string? expectedPayloadTypeSuffix)
    {
        FindingsSnapshot snapshot = new()
        {
            SchemaVersion = FindingsSchema.CurrentSnapshotVersion,
            Findings =
            {
                new Finding
                {
                    FindingSchemaVersion = 0,
                    FindingType = findingType,
                    FindingId = "x",
                    EngineType = "engine",
                    Title = "title",
                    Rationale = "why",
                    Category = "",
                    PayloadType = ""
                }
            }
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        Finding f = snapshot.Findings[0];
        f.FindingSchemaVersion.Should().Be(FindingsSchema.CurrentFindingVersion);
        f.Category.Should().Be(expectedCategory);

        if (expectedPayloadTypeSuffix is null)

            f.PayloadType.Should().BeNull();

        else
        {
            f.PayloadType.Should().NotBeNull();
            f.PayloadType!.Should().Contain(expectedPayloadTypeSuffix);
        }
    }

    [Fact]
    public void Apply_BumpsSchemaVersion_WhenLowerThanCurrent()
    {
        FindingsSnapshot snapshot = new()
        {
            SchemaVersion = 0,
            Findings =
            {
                new Finding
                {
                    FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                    FindingType = "RequirementFinding",
                    FindingId = "id",
                    EngineType = "e",
                    Title = "t",
                    Rationale = "r",
                    Category = "Requirement",
                    PayloadType = nameof(RequirementFindingPayload)
                }
            }
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        snapshot.SchemaVersion.Should().Be(FindingsSchema.CurrentSnapshotVersion);
    }
}

using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class FindingsSnapshotMigratorTests
{
    [Fact]
    public void Apply_FillsCategoryAndPayloadType_ForLegacyFinding()
    {
        FindingsSnapshot snapshot = new()
        {
            Findings =
            {
                new Finding
                {
                    FindingSchemaVersion = 0,
                    FindingType = "RequirementFinding",
                    FindingId = "x",
                    EngineType = "e",
                    Title = "t",
                    Rationale = "r",
                    Category = "",
                    PayloadType = null
                }
            }
        };

        FindingsSnapshotMigrator.Apply(snapshot);

        Finding f = snapshot.Findings[0];
        f.FindingSchemaVersion.Should().Be(FindingsSchema.CurrentFindingVersion);
        f.Category.Should().Be("Requirement");
        f.PayloadType.Should().Contain("Requirement");
    }
}

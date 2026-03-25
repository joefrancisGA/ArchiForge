using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class FindingsSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesTypedPayload()
    {
        FindingsSnapshot snapshot = new()
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            Findings =
            {
                FindingFactory.CreateRequirementFinding("req", "title", "rat", "Name", "Text", false)
            }
        };

        string json = FindingsSerialization.SerializeSnapshot(snapshot);
        FindingsSnapshot back = FindingsSerialization.DeserializeSnapshot(json);

        back.Findings.Should().HaveCount(1);
        RequirementFindingPayload? p = FindingPayloadConverter.ToRequirementPayload(back.Findings[0]);
        p!.RequirementName.Should().Be("Name");
        p.RequirementText.Should().Be("Text");
    }
}

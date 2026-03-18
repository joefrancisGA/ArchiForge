using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Findings.Serialization;
using ArchiForge.Decisioning.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Decisioning.Tests;

public sealed class FindingsSerializationTests
{
    [Fact]
    public void RoundTrip_PreservesTypedPayload()
    {
        var snapshot = new FindingsSnapshot
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

        var json = FindingsSerialization.SerializeSnapshot(snapshot);
        var back = FindingsSerialization.DeserializeSnapshot(json);

        back.Findings.Should().HaveCount(1);
        var p = FindingPayloadConverter.ToRequirementPayload(back.Findings[0]);
        p!.RequirementName.Should().Be("Name");
        p.RequirementText.Should().Be("Text");
    }
}

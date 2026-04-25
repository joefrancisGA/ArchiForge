using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Findings;
using ArchLucid.Persistence.Serialization;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Findings;

public sealed class FindingsSnapshotLegacyJsonReaderTests
{
    [Fact]
    public void DeserializeFindings_when_json_null_or_whitespace_returns_empty()
    {
        FindingsSnapshotLegacyJsonReader.DeserializeFindings(null).Should().BeEmpty();
        FindingsSnapshotLegacyJsonReader.DeserializeFindings("").Should().BeEmpty();
        FindingsSnapshotLegacyJsonReader.DeserializeFindings("   ").Should().BeEmpty();
    }

    [Fact]
    public void DeserializeFindings_round_trips_full_snapshot_blob()
    {
        FindingsSnapshot original = new()
        {
            FindingsSnapshotId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            RunId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ContextSnapshotId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            GraphSnapshotId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            CreatedUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            SchemaVersion = 1,
            Findings =
            [
                new Finding
                {
                    FindingId = "legacy",
                    FindingType = "TopologyGap",
                    Category = "Topology",
                    EngineType = "E",
                    Severity = FindingSeverity.Info,
                    Title = "Legacy title",
                    Rationale = "Legacy"
                }
            ]
        };

        string json = JsonEntitySerializer.Serialize(original);

        List<Finding> parsed = FindingsSnapshotLegacyJsonReader.DeserializeFindings(json);

        parsed.Should().ContainSingle(f => f.FindingId == "legacy");
        parsed[0].Title.Should().Be("Legacy title");
    }

    [Fact]
    public void DeserializeFindings_invalid_json_returns_empty()
    {
        FindingsSnapshotLegacyJsonReader.DeserializeFindings("{not json").Should().BeEmpty();
    }
}

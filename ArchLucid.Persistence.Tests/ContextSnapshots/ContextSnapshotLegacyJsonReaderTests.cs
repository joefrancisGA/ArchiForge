using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.ContextSnapshots;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.Tests.ContextSnapshots;

public sealed class ContextSnapshotLegacyJsonReaderTests
{
    [SkippableFact]
    public void DeserializeCanonicalObjects_when_json_null_or_whitespace_returns_empty()
    {
        ContextSnapshotLegacyJsonReader.DeserializeCanonicalObjects(null).Should().BeEmpty();
        ContextSnapshotLegacyJsonReader.DeserializeCanonicalObjects(string.Empty).Should().BeEmpty();
        ContextSnapshotLegacyJsonReader.DeserializeCanonicalObjects("   ").Should().BeEmpty();
    }

    [SkippableFact]
    public void DeserializeStringList_when_json_null_or_whitespace_returns_empty()
    {
        ContextSnapshotLegacyJsonReader.DeserializeStringList(null).Should().BeEmpty();
        ContextSnapshotLegacyJsonReader.DeserializeStringList("").Should().BeEmpty();
    }

    [SkippableFact]
    public void DeserializeSourceHashes_when_json_null_or_whitespace_returns_empty_ordinal_dict()
    {
        Dictionary<string, string> empty = ContextSnapshotLegacyJsonReader.DeserializeSourceHashes(null);

        empty.Should().BeEmpty();
        empty.Comparer.Should().Be(StringComparer.Ordinal);
    }

    [SkippableFact]
    public void Deserialize_round_trips_like_integration_legacy_row()
    {
        List<CanonicalObject> canonical =
        [
            new()
            {
                ObjectId = "legacy-obj",
                ObjectType = "Type",
                Name = "Legacy",
                SourceType = "S",
                SourceId = "sid",
                Properties = []
            }
        ];

        List<string> warnings = ["jw"];

        Dictionary<string, string> hashes = new(StringComparer.Ordinal) { ["a.cs"] = "h1" };

        List<CanonicalObject> c2 = ContextSnapshotLegacyJsonReader.DeserializeCanonicalObjects(
            JsonEntitySerializer.Serialize(canonical));

        List<string> w2 =
            ContextSnapshotLegacyJsonReader.DeserializeStringList(JsonEntitySerializer.Serialize(warnings));

        Dictionary<string, string> h2 =
            ContextSnapshotLegacyJsonReader.DeserializeSourceHashes(JsonEntitySerializer.Serialize(hashes));

        c2.Should().ContainSingle(o => o.ObjectId == "legacy-obj");
        w2.Should().Equal("jw");
        h2.Should().ContainKey("a.cs").WhoseValue.Should().Be("h1");
        h2.Comparer.Should().Be(StringComparer.Ordinal);
    }
}

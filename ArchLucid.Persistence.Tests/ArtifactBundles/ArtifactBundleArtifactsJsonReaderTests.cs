using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Persistence.ArtifactBundles;
using ArchLucid.Persistence.Serialization;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.ArtifactBundles;

public sealed class ArtifactBundleArtifactsJsonReaderTests
{
    [Fact]
    public void DeserializeArtifacts_when_json_null_or_whitespace_returns_empty_list()
    {
        ArtifactBundleArtifactsJsonReader.DeserializeArtifacts(null).Should().BeEmpty();
        ArtifactBundleArtifactsJsonReader.DeserializeArtifacts(string.Empty).Should().BeEmpty();
        ArtifactBundleArtifactsJsonReader.DeserializeArtifacts("   ").Should().BeEmpty();
    }

    [Fact]
    public void DeserializeArtifacts_round_trips_serialized_list()
    {
        List<SynthesizedArtifact> original =
        [
            new()
            {
                ArtifactId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                RunId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ManifestId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                CreatedUtc = new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc),
                ArtifactType = "t",
                Name = "n",
                Format = "f",
                Content = "body",
                ContentHash = "h",
                Metadata = new Dictionary<string, string> { ["k"] = "v" },
                ContributingDecisionIds = ["d"]
            }
        ];

        string json = JsonEntitySerializer.Serialize(original);

        List<SynthesizedArtifact> parsed = ArtifactBundleArtifactsJsonReader.DeserializeArtifacts(json);

        parsed.Should().ContainSingle();
        parsed[0].Content.Should().Be("body");
        parsed[0].Metadata.Should().ContainKey("k").WhoseValue.Should().Be("v");
        parsed[0].ContributingDecisionIds.Should().Equal("d");
    }
}

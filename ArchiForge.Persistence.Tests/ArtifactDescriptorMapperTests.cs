using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;
using ArchiForge.Persistence.Queries;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

/// <summary>
/// Ensures operator/UI-facing artifact ordering stays deterministic (name, then id).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArtifactDescriptorMapperTests
{
    private static SynthesizedArtifact Artifact(Guid id, string name) =>
        new()
        {
            ArtifactId = id,
            RunId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            ManifestId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            CreatedUtc = DateTime.UtcNow,
            ArtifactType = "Test",
            Name = name,
            Format = "json",
            Content = "{}",
            ContentHash = "abc",
        };

    [Fact]
    public void OrderSynthesizedArtifacts_SortsByNameCaseInsensitiveThenArtifactId()
    {
        Guid idA = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid idB = Guid.Parse("00000000-0000-0000-0000-000000000001");
        SynthesizedArtifact[] input =
        [
            Artifact(idA, "same"),
            Artifact(idB, "same"),
            Artifact(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Alpha"),
        ];

        IReadOnlyList<SynthesizedArtifact> ordered = ArtifactDescriptorMapper.OrderSynthesizedArtifacts(input);

        ordered[0].Name.Should().Be("Alpha");
        ordered[1].Name.Should().Be("same");
        ordered[2].Name.Should().Be("same");
        ordered[1].ArtifactId.Should().Be(idB);
        ordered[2].ArtifactId.Should().Be(idA);
    }

    [Fact]
    public void ToDescriptorList_PreservesSameOrderAsOrderSynthesizedArtifacts()
    {
        Guid id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        SynthesizedArtifact[] input = [Artifact(id2, "z"), Artifact(id1, "a")];

        IReadOnlyList<ArtifactDescriptor> descriptors = ArtifactDescriptorMapper.ToDescriptorList(input);

        descriptors.Should().HaveCount(2);
        descriptors[0].Name.Should().Be("a");
        descriptors[1].Name.Should().Be("z");
        descriptors[0].ArtifactId.Should().Be(id1);
        descriptors[1].ArtifactId.Should().Be(id2);
    }
}

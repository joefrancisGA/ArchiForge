using System.Text.Json;

using ArchLucid.Core.Explanation;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Explanation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class StructuredExplanationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Json_round_trip_preserves_all_fields()
    {
        StructuredExplanation original = new()
        {
            SchemaVersion = 1,
            Reasoning = "Because the graph shows a single ingress.",
            EvidenceRefs = ["edge-1", "node-2"],
            Confidence = 0.82m,
            AlternativesConsidered = ["Keep monolith"],
            Caveats = ["Assumes latest scan"],
        };

        string json = JsonSerializer.Serialize(original, JsonOptions);
        StructuredExplanation? back = JsonSerializer.Deserialize<StructuredExplanation>(json, JsonOptions);

        back.Should().NotBeNull();
        back!.SchemaVersion.Should().Be(1);
        back.Reasoning.Should().Be(original.Reasoning);
        back.EvidenceRefs.Should().Equal("edge-1", "node-2");
        back.Confidence.Should().Be(0.82m);
        back.AlternativesConsidered.Should().Equal("Keep monolith");
        back.Caveats.Should().Equal("Assumes latest scan");
    }

    [Fact]
    public void Defaults_schema_version_one_empty_evidence_null_optionals()
    {
        StructuredExplanation sut = new()
        {
            Reasoning = "x",
        };

        sut.SchemaVersion.Should().Be(1);
        sut.EvidenceRefs.Should().BeEmpty();
        sut.Confidence.Should().BeNull();
        sut.AlternativesConsidered.Should().BeNull();
        sut.Caveats.Should().BeNull();
    }
}

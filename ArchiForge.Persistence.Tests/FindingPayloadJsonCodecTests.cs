using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Persistence.Findings;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests;

[Trait("Category", "Unit")]
public sealed class FindingPayloadJsonCodecTests
{
    [Fact]
    public void SerializeDeserialize_round_trips_registered_payload_type()
    {
        RequirementFindingPayload original = new()
        {
            RequirementText = "Must use TLS 1.2+",
            RequirementName = "TLS",
            IsMandatory = true,
        };

        string? json = FindingPayloadJsonCodec.SerializePayload(original);
        json.Should().NotBeNullOrWhiteSpace();

        object? restored = FindingPayloadJsonCodec.DeserializePayload(json, nameof(RequirementFindingPayload));
        restored.Should().BeOfType<RequirementFindingPayload>();
        RequirementFindingPayload typed = (RequirementFindingPayload)restored;
        typed.RequirementName.Should().Be("TLS");
        typed.RequirementText.Should().Be("Must use TLS 1.2+");
        typed.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public void SerializePayload_null_yields_null()
    {
        FindingPayloadJsonCodec.SerializePayload(null).Should().BeNull();
    }

    [Fact]
    public void DeserializePayload_null_or_whitespace_yields_null()
    {
        FindingPayloadJsonCodec.DeserializePayload(null, "x").Should().BeNull();
        FindingPayloadJsonCodec.DeserializePayload("   ", "x").Should().BeNull();
    }
}

using System.Text.Json;

using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Findings;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Tests for Finding Payload Json Codec.
/// </summary>
[Trait("Category", "Unit")]
public sealed class FindingPayloadJsonCodecTests
{
    [Fact]
    public void SerializeDeserialize_round_trips_registered_payload_type()
    {
        RequirementFindingPayload original = new()
        {
            RequirementText = "Must use TLS 1.2+", RequirementName = "TLS", IsMandatory = true
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

    [Fact]
    public void HydrateJsonElementPayloads_replaces_JsonElement_with_typed_payload()
    {
        using JsonDocument doc = JsonDocument.Parse(
            """{"RequirementText":"Must","RequirementName":"R1","IsMandatory":true}""");

        Finding finding = new() { Payload = doc.RootElement.Clone(), PayloadType = nameof(RequirementFindingPayload) };

        FindingPayloadJsonCodec.HydrateJsonElementPayloads([finding]);

        finding.Payload.Should().BeOfType<RequirementFindingPayload>();
        RequirementFindingPayload typed = (RequirementFindingPayload)finding.Payload;
        typed.RequirementName.Should().Be("R1");
        typed.RequirementText.Should().Be("Must");
        typed.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public void HydrateJsonElementPayloads_leaves_non_JsonElement_unchanged()
    {
        RequirementFindingPayload original = new() { RequirementName = "n" };
        Finding finding = new() { Payload = original, PayloadType = nameof(RequirementFindingPayload) };

        FindingPayloadJsonCodec.HydrateJsonElementPayloads([finding]);

        finding.Payload.Should().BeSameAs(original);
    }

    [Fact]
    public void HydrateJsonElementPayloads_null_findings_throws()
    {
        Action act = () => FindingPayloadJsonCodec.HydrateJsonElementPayloads(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

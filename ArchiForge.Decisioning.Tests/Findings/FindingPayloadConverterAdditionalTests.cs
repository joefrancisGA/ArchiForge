using System.Text.Json;

using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests.Findings;

[Trait("Category", "Unit")]
public sealed class FindingPayloadConverterAdditionalTests
{
    [Fact]
    public void ConvertPayload_NullFinding_ThrowsArgumentNullException()
    {
        Action act = () => FindingPayloadConverter.ConvertPayload<RequirementFindingPayload>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("finding");
    }

    [Fact]
    public void ConvertPayload_NullPayload_ReturnsDefault()
    {
        Finding finding = new()
        {
            FindingId = "id-1",
            Payload = null
        };

        RequirementFindingPayload? result = FindingPayloadConverter.ConvertPayload<RequirementFindingPayload>(finding);

        result.Should().BeNull();
    }

    [Fact]
    public void ConvertPayload_JsonElement_DeserializesCorrectly()
    {
        RequirementFindingPayload source = new()
        {
            RequirementName = "N",
            RequirementText = "T",
            IsMandatory = true
        };
        JsonElement element = JsonSerializer.SerializeToElement(source);

        Finding finding = new()
        {
            FindingId = "id-json",
            Payload = element
        };

        RequirementFindingPayload? result = FindingPayloadConverter.ConvertPayload<RequirementFindingPayload>(finding);

        result.Should().NotBeNull();
        result!.RequirementName.Should().Be("N");
        result.RequirementText.Should().Be("T");
        result.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public void ConvertPayload_AnonymousObject_RoundTripsThroughJson()
    {
        object payload = new
        {
            RequirementName = "FromAnon",
            RequirementText = "Text",
            IsMandatory = false
        };

        Finding finding = new()
        {
            FindingId = "id-anon",
            Payload = payload
        };

        RequirementFindingPayload? result = FindingPayloadConverter.ConvertPayload<RequirementFindingPayload>(finding);

        result.Should().NotBeNull();
        result!.RequirementName.Should().Be("FromAnon");
        result.RequirementText.Should().Be("Text");
        result.IsMandatory.Should().BeFalse();
    }

    [Fact]
    public void ConvertPayload_CorruptJsonElement_ThrowsInvalidOperationException()
    {
        JsonElement corrupt = JsonSerializer.SerializeToElement(42);

        Finding finding = new()
        {
            FindingId = "bad-payload",
            Payload = corrupt
        };

        Action act = () => FindingPayloadConverter.ConvertPayload<RequirementFindingPayload>(finding);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*RequirementFindingPayload*FindingId=bad-payload*");
    }
}

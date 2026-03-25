using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class FindingPayloadConverterTests
{
    [Fact]
    public void ToRequirementPayload_FromStronglyTypedObject()
    {
        Finding f = new()
        {
            PayloadType = nameof(RequirementFindingPayload),
            Payload = new RequirementFindingPayload
            {
                RequirementName = "A",
                RequirementText = "B",
                IsMandatory = true
            }
        };

        RequirementFindingPayload? p = FindingPayloadConverter.ToRequirementPayload(f);
        p!.RequirementName.Should().Be("A");
        p.RequirementText.Should().Be("B");
    }
}

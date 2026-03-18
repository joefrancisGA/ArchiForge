using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Decisioning.Tests;

public sealed class FindingPayloadConverterTests
{
    [Fact]
    public void ToRequirementPayload_FromStronglyTypedObject()
    {
        var f = new Finding
        {
            PayloadType = nameof(RequirementFindingPayload),
            Payload = new RequirementFindingPayload
            {
                RequirementName = "A",
                RequirementText = "B",
                IsMandatory = true
            }
        };

        var p = FindingPayloadConverter.ToRequirementPayload(f);
        p!.RequirementName.Should().Be("A");
        p.RequirementText.Should().Be("B");
    }
}

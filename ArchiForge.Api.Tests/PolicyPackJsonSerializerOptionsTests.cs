using System.Text.Json;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class PolicyPackJsonSerializerOptionsTests
{
    [Fact]
    public void Default_RoundTripsPolicyPackContentDocument_WithCamelCase()
    {
        var doc = new PolicyPackContentDocument
        {
            AlertRuleIds = [Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")],
            AdvisoryDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key"] = "value",
            },
        };

        var json = JsonSerializer.Serialize(doc, PolicyPackJsonSerializerOptions.Default);
        json.Should().Contain("alertRuleIds");

        var back = JsonSerializer.Deserialize<PolicyPackContentDocument>(json, PolicyPackJsonSerializerOptions.Default);
        back.Should().NotBeNull();
        back.AlertRuleIds.Should().ContainSingle().Which.Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        back.AdvisoryDefaults["key"].Should().Be("value");
    }
}

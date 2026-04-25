using System.Text.Json;

using ArchLucid.Decisioning.Governance.PolicyPacks;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Policy Pack Json Serializer Options.
/// </summary>
[Trait("Category", "Unit")]
public sealed class PolicyPackJsonSerializerOptionsTests
{
    [Fact]
    public void Default_RoundTripsPolicyPackContentDocument_WithCamelCase()
    {
        PolicyPackContentDocument doc = new()
        {
            AlertRuleIds = [Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")],
            AdvisoryDefaults =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["key"] = "value" }
        };

        string json = JsonSerializer.Serialize(doc, PolicyPackJsonSerializerOptions.Default);
        json.Should().Contain("alertRuleIds");

        PolicyPackContentDocument? back =
            JsonSerializer.Deserialize<PolicyPackContentDocument>(json, PolicyPackJsonSerializerOptions.Default);
        back.Should().NotBeNull();
        back.AlertRuleIds.Should().ContainSingle().Which.Should()
            .Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        back.AdvisoryDefaults["key"].Should().Be("value");
    }
}

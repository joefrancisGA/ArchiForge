using System.Text.Json;

using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Round-trip and merge-shape checks for governance JSON documents used by the API stack.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GovernancePolicyPackContentPropertyTests
{
    [Theory]
    [InlineData("env", "prod")]
    [InlineData("tier", "enterprise")]
    [InlineData("key-with-dashes", "v1")]
    [InlineData("unicode-key", "值")]
    [InlineData("quotes", "say \"hi\"")]
    public void Policy_pack_content_metadata_entry_round_trips_through_shared_options(string key, string value)
    {
        PolicyPackContentDocument original = new()
        {
            Metadata = { [key] = value }
        };

        string json = JsonSerializer.Serialize(original, PolicyPackJsonSerializerOptions.Default);
        PolicyPackContentDocument? back = JsonSerializer.Deserialize<PolicyPackContentDocument>(
            json,
            PolicyPackJsonSerializerOptions.Default);

        Assert.NotNull(back);
        Assert.True(back.Metadata.TryGetValue(key, out string? read));
        Assert.Equal(value, read);
    }
}

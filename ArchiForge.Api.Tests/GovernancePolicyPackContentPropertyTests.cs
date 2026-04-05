using System.Text.Json;

using ArchiForge.Decisioning.Governance.PolicyPacks;

using FsCheck;
using FsCheck.Xunit;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Property checks for governance JSON documents that flow through the API stack.
/// </summary>
[Trait("Suite", "Core")]
public sealed class GovernancePolicyPackContentPropertyTests
{
    [Property(MaxTest = 40)]
    public void Policy_pack_content_metadata_entry_round_trips_through_shared_options(NonEmptyString key, NonEmptyString value)
    {
        string k = key.Get;
        string v = value.Get;

        PolicyPackContentDocument original = new();
        original.Metadata[k] = v;

        string json = JsonSerializer.Serialize(original, PolicyPackJsonSerializerOptions.Default);
        PolicyPackContentDocument? back = JsonSerializer.Deserialize<PolicyPackContentDocument>(
            json,
            PolicyPackJsonSerializerOptions.Default);

        Assert.NotNull(back);
        Assert.True(back!.Metadata.TryGetValue(k, out string? read));
        Assert.Equal(v, read);
    }
}

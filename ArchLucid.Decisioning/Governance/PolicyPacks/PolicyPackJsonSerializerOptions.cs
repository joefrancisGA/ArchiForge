using System.Text.Json;

using ArchLucid.Decisioning.Governance.Resolution;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Shared <see cref="JsonSerializerOptions" /> for serializing/deserializing <see cref="PolicyPackContentDocument" />
///     and pack <c>ContentJson</c>.
/// </summary>
/// <remarks>
///     <strong>Why static:</strong> avoids allocating new options per IO operation (analyzers / performance). Do not
///     mutate after first use.
///     Used by <see cref="EffectiveGovernanceResolver" />, loaders, and API surfaces that round-trip JSON.
/// </remarks>
public static class PolicyPackJsonSerializerOptions
{
    /// <summary>Case-insensitive read, camelCase write, trailing commas and comments allowed on read.</summary>
    public static JsonSerializerOptions Default
    {
        get;
    } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

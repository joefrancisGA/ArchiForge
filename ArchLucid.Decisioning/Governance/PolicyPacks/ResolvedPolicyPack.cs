namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>One entry in <see cref="EffectivePolicyPackSet.Packs" /> after assignment + version resolution.</summary>
/// <remarks>
///     <see cref="ContentJson" /> is opaque to this DTO; clients deserialize with
///     <see cref="PolicyPackJsonSerializerOptions" /> when needed.
/// </remarks>
public class ResolvedPolicyPack
{
    /// <summary>Pack id.</summary>
    public Guid PolicyPackId
    {
        get;
        set;
    }

    /// <summary>Pack display name.</summary>
    public string Name
    {
        get;
        set;
    } = null!;

    /// <summary>Resolved version string.</summary>
    public string Version
    {
        get;
        set;
    } = null!;

    /// <summary><see cref="PolicyPackType" /> discriminator.</summary>
    public string PackType
    {
        get;
        set;
    } = null!;

    /// <summary>Raw JSON for <see cref="PolicyPackContentDocument" />.</summary>
    public string ContentJson
    {
        get;
        set;
    } = null!;
}

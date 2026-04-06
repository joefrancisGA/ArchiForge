namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>Lifecycle states for <see cref="PolicyPack.Status"/>.</summary>
/// <remarks>Transitions: Draft → Active on first publish; Retired reserved for future soft-delete flows.</remarks>
public static class PolicyPackStatus
{
    /// <summary>Newly created pack before first publish.</summary>
    public const string Draft = "Draft";

    /// <summary>At least one version published; pack is assignable in normal flows.</summary>
    public const string Active = "Active";

    /// <summary>Pack no longer intended for new assignments (optional future use).</summary>
    public const string Retired = "Retired";
}

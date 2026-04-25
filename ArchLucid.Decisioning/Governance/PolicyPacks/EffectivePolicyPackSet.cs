namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Result of <see cref="IPolicyPackResolver.ResolveAsync" />: every enabled, applicable pack as its own
///     <see cref="ResolvedPolicyPack" /> (no merge).
/// </summary>
/// <remarks>
///     Returned by <c>GET …/policy-packs/effective</c>. For merged content use <see cref="IEffectiveGovernanceLoader" />
///     or <see cref="Resolution.IEffectiveGovernanceResolver" />.
/// </remarks>
public class EffectivePolicyPackSet
{
    /// <summary>Echo of request scope.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Echo of request scope.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Echo of request scope.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Ordered list of resolved packs (repository order).</summary>
    public List<ResolvedPolicyPack> Packs
    {
        get;
        set;
    } = [];
}

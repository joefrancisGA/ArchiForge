namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>Versioned governance bundle metadata (name, type, lifecycle) scoped to tenant/workspace/project.</summary>
/// <remarks>
///     Created via <see cref="IPolicyPackManagementService.CreatePackAsync" />; status moves to
///     <see cref="PolicyPackStatus.Active" /> on publish.
///     Referenced when resolving assignments to display names in <see cref="ResolvedPolicyPack" /> and governance
///     decisions.
/// </remarks>
public class PolicyPack
{
    /// <summary>Surrogate key.</summary>
    public Guid PolicyPackId
    {
        get;
        set;
    } = Guid.NewGuid();

    /// <summary>Pack authoring scope (not necessarily assignment scope).</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Pack authoring scope.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Pack authoring scope.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Human-readable title.</summary>
    public string Name
    {
        get;
        set;
    } = null!;

    /// <summary>Optional longer description.</summary>
    public string Description
    {
        get;
        set;
    } = null!;

    /// <summary>One of <see cref="PolicyPackType" />.</summary>
    public string PackType
    {
        get;
        set;
    } = PolicyPackType.BuiltIn;

    /// <summary>Lifecycle: <see cref="PolicyPackStatus" />.</summary>
    public string Status
    {
        get;
        set;
    } = PolicyPackStatus.Draft;

    /// <summary>Row creation time.</summary>
    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>Set when the pack first becomes active (publish).</summary>
    public DateTime? ActivatedUtc
    {
        get;
        set;
    }

    /// <summary>Latest published / current version label (denormalized for list UIs).</summary>
    public string CurrentVersion
    {
        get;
        set;
    } = "1.0.0";
}

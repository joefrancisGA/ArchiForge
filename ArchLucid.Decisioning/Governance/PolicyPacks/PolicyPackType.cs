namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Classification of <see cref="PolicyPack.PackType" /> for UI and policy (built-in vs tenant/workspace/project
///     custom).
/// </summary>
public static class PolicyPackType
{
    /// <summary>Platform-provided template packs.</summary>
    public const string BuiltIn = "BuiltIn";

    /// <summary>Tenant-scoped custom pack.</summary>
    public const string TenantCustom = "TenantCustom";

    /// <summary>Workspace-scoped custom pack.</summary>
    public const string WorkspaceCustom = "WorkspaceCustom";

    /// <summary>Project-scoped custom pack.</summary>
    public const string ProjectCustom = "ProjectCustom";
}

namespace ArchLucid.Core.Tenancy;

/// <summary>First workspace row for a tenant (used after provisioning / idempotent replay).</summary>
public sealed class TenantWorkspaceLink
{
    public Guid WorkspaceId { get; init; }

    public Guid DefaultProjectId { get; init; }
}

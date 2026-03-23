namespace ArchiForge.Core.Scoping;

/// <summary>
/// Identifies the current tenant, workspace, and project for multi-tenant APIs and background jobs.
/// </summary>
/// <remarks>
/// Produced by <see cref="IScopeContextProvider"/> (HTTP claims/headers) or <see cref="AmbientScopeContext"/> overrides.
/// Governance, compliance, and alert services key repositories and effective policy resolution on these ids.
/// </remarks>
public sealed class ScopeContext
{
    /// <summary>Tenant id (required for scoped data).</summary>
    public Guid TenantId
    {
        get; set;
    }

    /// <summary>Workspace id within the tenant.</summary>
    public Guid WorkspaceId
    {
        get; set;
    }

    /// <summary>Project id within the workspace.</summary>
    public Guid ProjectId
    {
        get; set;
    }
}

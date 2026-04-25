namespace ArchLucid.Persistence.Models;

/// <summary>
///     Scope columns for a run row returned from archival so hot-path caches can evict cached run reads.
/// </summary>
public sealed class ArchivedRunScopeRow
{
    public Guid RunId
    {
        get;
        set;
    }

    public Guid TenantId
    {
        get;
        set;
    }

    public Guid WorkspaceId
    {
        get;
        set;
    }

    public Guid ScopeProjectId
    {
        get;
        set;
    }
}

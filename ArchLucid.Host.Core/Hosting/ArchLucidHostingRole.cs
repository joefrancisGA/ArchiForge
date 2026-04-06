namespace ArchiForge.Host.Core.Hosting;

/// <summary>
/// Process role for ArchiForge compute: public HTTP API only, background worker only, or both (local dev default).
/// </summary>
public enum ArchiForgeHostingRole
{
    /// <summary>HTTP API plus all hosted background services (default when <c>Hosting:Role</c> is unset).</summary>
    Combined = 0,

    /// <summary>Public API and in-process job queue only; no advisory scan, archival, or retrieval outbox loops (use with a separate Worker app).</summary>
    Api = 1,

    /// <summary>Hosted background services only (advisory scan, data archival, retrieval indexing outbox); minimal HTTP for health.</summary>
    Worker = 2,
}

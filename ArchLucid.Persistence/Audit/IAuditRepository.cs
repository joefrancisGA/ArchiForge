using ArchiForge.Core.Audit;

namespace ArchiForge.Persistence.Audit;

/// <summary>
/// Append-only persistence contract for <see cref="AuditEvent"/> records produced by
/// governance, alert, and run lifecycle actions.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Appends a single audit event. Implementations must never update or delete rows;
    /// this method is insert-only.
    /// </summary>
    /// <param name="auditEvent">The event to append.</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task AppendAsync(AuditEvent auditEvent, CancellationToken ct);

    /// <summary>
    /// Returns up to <paramref name="take"/> audit events for the given scope, ordered by
    /// event timestamp descending (most recent first).
    /// </summary>
    /// <param name="tenantId">Tenant boundary for the query.</param>
    /// <param name="workspaceId">Workspace boundary for the query.</param>
    /// <param name="projectId">Project boundary for the query.</param>
    /// <param name="take">Maximum number of rows to return (caller should clamp to a safe maximum).</param>
    /// <param name="ct">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct);
}

namespace ArchiForge.Core.Audit;

/// <summary>
/// Enriches and appends audit events (actor, scope, correlation). Implemented in the host (e.g. API).
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken ct);
}

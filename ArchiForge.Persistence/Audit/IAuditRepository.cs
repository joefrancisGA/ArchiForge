using ArchiForge.Core.Audit;

namespace ArchiForge.Persistence.Audit;

public interface IAuditRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken ct);

    Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct);
}

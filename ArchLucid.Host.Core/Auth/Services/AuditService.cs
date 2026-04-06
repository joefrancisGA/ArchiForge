using System.Diagnostics;
using System.Security.Claims;

using ArchiForge.Core.Audit;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Audit;

namespace ArchiForge.Host.Core.Auth.Services;

/// <summary>
/// Fills actor, scope, default DataJson, and correlation id before appending to the audit store.
/// </summary>
public sealed class AuditService(
    IAuditRepository repo,
    IHttpContextAccessor httpContextAccessor,
    IScopeContextProvider scopeProvider)
    : IAuditService
{
    public async Task LogAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        HttpContext? http = httpContextAccessor.HttpContext;
        ClaimsPrincipal? user = http?.User;
        ScopeContext scope = scopeProvider.GetCurrentScope();

        auditEvent.ActorUserId =
            user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        auditEvent.ActorUserName =
            user?.Identity?.Name ?? "unknown";

        auditEvent.TenantId = scope.TenantId;
        auditEvent.WorkspaceId = scope.WorkspaceId;
        auditEvent.ProjectId = scope.ProjectId;

        if (string.IsNullOrWhiteSpace(auditEvent.DataJson))
            auditEvent.DataJson = "{}";

        if (string.IsNullOrWhiteSpace(auditEvent.CorrelationId))
        {
            string? fromActivityChain = ActivityCorrelation.FindTagValueInChain(
                Activity.Current,
                ActivityCorrelation.LogicalCorrelationIdTag);

            auditEvent.CorrelationId =
                fromActivityChain
                ?? http?.TraceIdentifier
                ?? Activity.Current?.Id;
        }

        await repo.AppendAsync(auditEvent, ct);
    }
}

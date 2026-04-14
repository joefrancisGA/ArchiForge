using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
/// Dual-writes durable <see cref="AuditEventTypes.CoordinatorRunFailed"/> when coordinator paths record baseline <c>Architecture.RunFailed</c>.
/// </summary>
internal static class CoordinatorRunFailedDurableAudit
{
    public static async Task TryLogAsync(
        IAuditService auditService,
        IScopeContextProvider scopeProvider,
        ILogger logger,
        string actor,
        string correlationRunId,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            ScopeContext scope = scopeProvider.GetCurrentScope();
            Guid? runGuid = Guid.TryParse(correlationRunId, out Guid g) ? g : null;

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.CoordinatorRunFailed,
                    ActorUserId = actor,
                    ActorUserName = actor,
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    RunId = runGuid,
                    DataJson = JsonSerializer.Serialize(new { runId = correlationRunId, reason }),
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    ex,
                    "Durable audit for CoordinatorRunFailed failed for RunId={RunId}",
                    LogSanitizer.Sanitize(correlationRunId));
            }
        }
    }
}

using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Best-effort durable audit when <see cref="AgentResultSchemaViolationException"/> is thrown from an agent handler.
/// </summary>
internal static class AgentResultSchemaViolationAudit
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Schedules <see cref="IAuditService.LogAsync"/> without blocking the caller; swallows audit failures.
    /// </summary>
    public static void ScheduleLog(
        IAuditService auditService,
        IScopeContextProvider scopeProvider,
        AgentResultSchemaViolationException ex,
        string runId,
        string taskId,
        string? modelDeploymentName,
        string? modelVersion)
    {
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        try
        {
            ScopeContext scope = scopeProvider.GetCurrentScope();
            Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;

            List<string> errors = ex.SchemaErrors.Count <= 3
                ? [.. ex.SchemaErrors]
                : [.. ex.SchemaErrors.Take(3)];

            string dataJson = JsonSerializer.Serialize(
                new
                {
                    taskId,
                    agentType = ex.AgentType.ToString(),
                    errorCount = ex.SchemaErrors.Count,
                    errors,
                    modelDeploymentName,
                    modelVersion,
                },
                JsonOptions);

            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.AgentResultSchemaViolation,
                ActorUserId = "agent-runtime",
                ActorUserName = "agent-runtime",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = dataJson,
            };

            _ = auditService.LogAsync(auditEvent, CancellationToken.None).ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
        catch
        {
            // Never block agent failure path on audit scheduling.
        }
    }
}

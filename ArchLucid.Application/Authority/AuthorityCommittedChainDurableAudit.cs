using System.Diagnostics;
using System.Text.Json;
using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Authority;
/// <summary>
///     Appends <see cref="AuditEventTypes.AuthorityCommittedChainPersisted"/> after the authority snapshot chain
///     and golden manifest rows are committed (caller must invoke only after successful SQL persistence / UoW commit).
/// </summary>
public static class AuthorityCommittedChainDurableAudit
{
    public static async Task TryLogAsync(IAuditService auditService, IScopeContextProvider scopeProvider, IActorContext actorContext, ILogger logger, Guid authorityRunId, string projectSlug, AuthorityManifestPersistResult chainResult, string source, bool richFindingsAndGraph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        ArgumentNullException.ThrowIfNull(actorContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(projectSlug);
        ArgumentNullException.ThrowIfNull(chainResult);
        ArgumentNullException.ThrowIfNull(source);
        if (auditService is null)
            throw new ArgumentNullException(nameof(auditService));
        if (scopeProvider is null)
            throw new ArgumentNullException(nameof(scopeProvider));
        if (actorContext is null)
            throw new ArgumentNullException(nameof(actorContext));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        try
        {
            string actor = actorContext.GetActor();
            ScopeContext scope = scopeProvider.GetCurrentScope();
            string correlationId = Activity.Current?.Id ?? $"{LogSanitizer.Sanitize(source)}:{authorityRunId:N}";
            object payload = new
            {
                source = LogSanitizer.Sanitize(source),
                projectSlug = LogSanitizer.Sanitize(projectSlug),
                richFindingsAndGraph,
                contextSnapshotId = chainResult.ContextSnapshotId,
                graphSnapshotId = chainResult.GraphSnapshotId,
                findingsSnapshotId = chainResult.FindingsSnapshotId,
                decisionTraceId = chainResult.DecisionTraceId,
                manifestId = chainResult.GoldenManifestId
            };
            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.AuthorityCommittedChainPersisted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = authorityRunId,
                ManifestId = chainResult.GoldenManifestId,
                DataJson = JsonSerializer.Serialize(payload),
                CorrelationId = correlationId
            };
            await auditService.LogAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(ex, "Durable audit for AuthorityCommittedChainPersisted failed for RunId={RunId}", LogSanitizer.Sanitize(authorityRunId.ToString("N")));
        }
    }
}
using System.Text.Json;
using System.Text.RegularExpressions;

using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Common;

/// <summary>
/// Universal durable dual-write for <see cref="AuditEventTypes.Baseline.Architecture"/> baseline events:
/// mirrors legacy <see cref="CoordinatorRunCatalogDurableDualWrite"/> / <see cref="CoordinatorRunFailedDurableAudit"/>
/// payloads from <see cref="IBaselineMutationAuditService.RecordAsync"/> arguments.
/// </summary>
internal static class BaselineMutationAuditArchitectureDurableWriter
{
    public static async Task TryWriteArchitectureDurableEchoAsync(
        string eventType,
        string actor,
        string entityId,
        string details,
        IAuditService auditService,
        IScopeContextProvider scopeContextProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (auditService is null)
            throw new ArgumentNullException(nameof(auditService));

        if (scopeContextProvider is null)
            throw new ArgumentNullException(nameof(scopeContextProvider));

        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        if (string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunFailed, StringComparison.Ordinal))
        {
            await CoordinatorRunFailedDurableAudit.TryLogAsync(
                auditService,
                scopeContextProvider,
                logger,
                actor,
                entityId,
                details,
                cancellationToken);

            return;
        }

        if (string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunCreated, StringComparison.Ordinal))
        {
            await DurableAuditLogRetry.TryLogAsync(
                async ct =>
                {
                    ScopeContext scope = scopeContextProvider.GetCurrentScope();
                    Guid? runGuid = Guid.TryParse(entityId, out Guid rid) ? rid : null;
                    Dictionary<string, string> kv = ParseSemicolonKeyValues(details);

                    string requestId = GetDetail(kv, "RequestId");
                    string systemName = GetDetail(kv, "SystemName");

                    AuditEvent legacyCreated = new()
                    {
                        EventType = AuditEventTypes.CoordinatorRunCreated,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        RunId = runGuid,
                        DataJson = JsonSerializer.Serialize(new { requestId, systemName }),
                    };

                    await CoordinatorRunCatalogDurableDualWrite.LogTwiceAsync(
                        auditService,
                        legacyCreated,
                        AuditEventTypes.Run.Created,
                        ct);
                },
                logger,
                $"CoordinatorRunCreated:{LogSanitizer.Sanitize(entityId)}",
                cancellationToken);

            return;
        }

        if (string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunStarted, StringComparison.Ordinal))
        {
            await DurableAuditLogRetry.TryLogAsync(
                async ct =>
                {
                    ScopeContext scope = scopeContextProvider.GetCurrentScope();
                    Guid? runGuid = Guid.TryParse(entityId, out Guid rid) ? rid : null;

                    AuditEvent legacyExecuteStarted = new()
                    {
                        EventType = AuditEventTypes.CoordinatorRunExecuteStarted,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        RunId = runGuid,
                        DataJson = JsonSerializer.Serialize(new { runId = entityId }),
                    };

                    await CoordinatorRunCatalogDurableDualWrite.LogTwiceAsync(
                        auditService,
                        legacyExecuteStarted,
                        AuditEventTypes.Run.ExecuteStarted,
                        ct);
                },
                logger,
                $"CoordinatorRunExecuteStarted:{LogSanitizer.Sanitize(entityId)}",
                cancellationToken);

            return;
        }

        if (string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded, StringComparison.Ordinal))
        {
            await DurableAuditLogRetry.TryLogAsync(
                async ct =>
                {
                    ScopeContext scope = scopeContextProvider.GetCurrentScope();
                    Guid? runGuid = Guid.TryParse(entityId, out Guid rid) ? rid : null;
                    int resultCount = TryParseResultCount(details);

                    AuditEvent legacyExecuteSucceeded = new()
                    {
                        EventType = AuditEventTypes.CoordinatorRunExecuteSucceeded,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        RunId = runGuid,
                        DataJson = JsonSerializer.Serialize(new { runId = entityId, resultCount }),
                    };

                    await CoordinatorRunCatalogDurableDualWrite.LogTwiceAsync(
                        auditService,
                        legacyExecuteSucceeded,
                        AuditEventTypes.Run.ExecuteSucceeded,
                        ct);
                },
                logger,
                $"CoordinatorRunExecuteSucceeded:{LogSanitizer.Sanitize(entityId)}",
                cancellationToken);

            return;
        }

        if (string.Equals(eventType, AuditEventTypes.Baseline.Architecture.RunCompleted, StringComparison.Ordinal))
        {
            await DurableAuditLogRetry.TryLogAsync(
                async ct =>
                {
                    ScopeContext scope = scopeContextProvider.GetCurrentScope();
                    Guid? runGuid = Guid.TryParse(entityId, out Guid rid) ? rid : null;
                    Dictionary<string, string> kv = ParseSemicolonKeyValues(details);
                    string manifestVersion = GetDetail(kv, "ManifestVersion");
                    string systemName = GetDetail(kv, "SystemName");
                    int warningCount = int.TryParse(GetDetail(kv, "WarningCount"), out int wc) ? wc : 0;
                    string? commitPath = GetDetailOrNull(kv, "CommitPath");

                    // Coordinator path historically omitted warningCount/commitPath in DataJson; authority adds commitPath (+ counts).
                    string commitJson = string.IsNullOrWhiteSpace(commitPath)
                        ? JsonSerializer.Serialize(new { runId = entityId, manifestVersion, systemName })
                        : JsonSerializer.Serialize(
                            new
                            {
                                runId = entityId,
                                manifestVersion,
                                systemName,
                                warningCount,
                                commitPath,
                            });

                    AuditEvent legacyCommitCompleted = new()
                    {
                        EventType = AuditEventTypes.CoordinatorRunCommitCompleted,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        RunId = runGuid,
                        DataJson = commitJson,
                    };

                    await CoordinatorRunCatalogDurableDualWrite.LogTwiceAsync(
                        auditService,
                        legacyCommitCompleted,
                        AuditEventTypes.Run.CommitCompleted,
                        ct);
                },
                logger,
                $"CoordinatorRunCommitCompleted:{LogSanitizer.Sanitize(entityId)}",
                cancellationToken);

            return;
        }
    }

    private static string GetDetail(Dictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out string? v) ? v : string.Empty;
    }

    private static string? GetDetailOrNull(Dictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out string? v) ? v : null;
    }

    private static Dictionary<string, string> ParseSemicolonKeyValues(string details)
    {
        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(details))
            return map;

        foreach (string segment in details.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = segment.IndexOf('=');

            if (eq <= 0 || eq >= segment.Length - 1)
                continue;

            string key = segment[..eq].Trim();
            string value = segment[(eq + 1)..].Trim();

            if (key.Length > 0)
                map[key] = value;
        }

        return map;
    }


    private static int TryParseResultCount(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return 0;

        Match m = Regex.Match(details, @"ResultCount\s*=\s*(\d+)", RegexOptions.IgnoreCase);

        return m.Success && int.TryParse(m.Groups[1].Value, out int n) ? n : 0;
    }
}

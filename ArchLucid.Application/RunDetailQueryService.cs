using ArchLucid.Application.Runs.Mapping;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application;
/// <summary>
///     Assembles the canonical <see cref = "ArchitectureRunDetail"/> from individual repositories.
///     This is the single, authoritative query path for run state — controllers, API application
///     services, analysis/export/compare, governance, and <see cref = "ReplayRunService"/> should use this
///     instead of assembling run metadata, tasks, results, manifest, and traces from repositories separately.
/// </summary>
/// <remarks>
///     ADR 0030 PR A3 (2026-04-24): the legacy <c>ICoordinatorDecisionTraceRepository</c> read path was
///     removed along with the interface itself. Decision traces are now read from
///     <see cref = "IDecisionTraceRepository">Decisioning.Interfaces.IDecisionTraceRepository</see> via
///     <see cref = "Persistence.Models.RunRecord.DecisionTraceId"/> on the run header — the authority FK
///     chain populates that pointer at commit time (<see cref = "ReplayRunService"/> + demo seed both go
///     through <c>IAuthorityCommittedManifestChainWriter.PersistCommittedChainAsync</c>).
/// </remarks>
public sealed class RunDetailQueryService(IRunRepository runRepository, IScopeContextProvider scopeContextProvider, IAgentTaskRepository taskRepository, IAgentResultRepository resultRepository, IUnifiedGoldenManifestReader unifiedGoldenManifestReader, IDecisionTraceRepository authorityDecisionTraceRepository, ILogger<RunDetailQueryService> logger) : IRunDetailQueryService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runRepository, scopeContextProvider, taskRepository, resultRepository, unifiedGoldenManifestReader, authorityDecisionTraceRepository, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Interfaces.IRunRepository runRepository, ArchLucid.Core.Scoping.IScopeContextProvider scopeContextProvider, ArchLucid.Persistence.Data.Repositories.IAgentTaskRepository taskRepository, ArchLucid.Persistence.Data.Repositories.IAgentResultRepository resultRepository, ArchLucid.Decisioning.Interfaces.IUnifiedGoldenManifestReader unifiedGoldenManifestReader, ArchLucid.Decisioning.Interfaces.IDecisionTraceRepository authorityDecisionTraceRepository, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.RunDetailQueryService> logger)
    {
        ArgumentNullException.ThrowIfNull(runRepository);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);
        ArgumentNullException.ThrowIfNull(taskRepository);
        ArgumentNullException.ThrowIfNull(resultRepository);
        ArgumentNullException.ThrowIfNull(unifiedGoldenManifestReader);
        ArgumentNullException.ThrowIfNull(authorityDecisionTraceRepository);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Contracts.Architecture.ArchitectureRunDetail?> GetRunDetailAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        if (!TryParseRunGuid(runId, out Guid runGuid))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("RunDetailQueryService: run '{RunId}' is not a valid run identifier.", LogSanitizer.Sanitize(runId));
            return null;
        }

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        RunRecord? record = await runRepository.GetByIdAsync(scope, runGuid, cancellationToken);
        if (record is null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("RunDetailQueryService: run '{RunId}' not found.", LogSanitizer.Sanitize(runId));
            return null;
        }

        IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        ArchitectureRun run = RunRecordToArchitectureRunMapper.ToArchitectureRun(record, tasks.Select(t => t.TaskId).ToList());
        IReadOnlyList<AgentResult> results = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
        GoldenManifest? manifest = await unifiedGoldenManifestReader.ReadByRunIdAsync(scope, runGuid, cancellationToken);
        List<DecisionTrace> decisionTraces = [];
        if (manifest is null)
        {
            if (!string.IsNullOrWhiteSpace(run.CurrentManifestVersion) && logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("RunDetailQueryService: run '{RunId}' references manifest version '{Version}' which no longer exists.", LogSanitizer.Sanitize(runId), LogSanitizer.Sanitize(run.CurrentManifestVersion));
        }
        else if (record.DecisionTraceId is { } authorityTraceId)
        {
            DecisionTrace? authorityTrace = await authorityDecisionTraceRepository.GetByIdAsync(scope, authorityTraceId, cancellationToken);
            if (authorityTrace is not null)
                decisionTraces = [authorityTrace];
        }

        return new ArchitectureRunDetail
        {
            Run = run,
            Tasks = tasks.ToList(),
            Results = results.ToList(),
            Manifest = manifest,
            DecisionTraces = decisionTraces,
            HasBrokenManifestReference = !string.IsNullOrWhiteSpace(run.CurrentManifestVersion) && manifest is null
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RunSummary>> ListRunSummariesAsync(CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        IReadOnlyList<RunRecord> records = await runRepository.ListRecentInScopeAsync(scope, 200, cancellationToken);
        return records.Select(r => new RunSummary { RunId = r.RunId.ToString("N"), RequestId = r.ArchitectureRequestId ?? string.Empty, Status = r.LegacyRunStatus ?? nameof(ArchitectureRunStatus.Created), CreatedUtc = r.CreatedUtc, CompletedUtc = r.CompletedUtc, CurrentManifestVersion = r.CurrentManifestVersion, SystemName = r.ProjectId }).ToList();
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<RunSummary> Items, bool HasMore, string? NextCursor)> ListRunSummariesKeysetAsync(string? cursor, int take, CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        DateTime? cursorUtc = null;
        Guid? cursorRunId = null;
        (DateTime CreatedUtc, Guid RunId)? decoded = RunCursorCodec.TryDecode(cursor);
        if (decoded.HasValue)
        {
            cursorUtc = decoded.Value.CreatedUtc;
            cursorRunId = decoded.Value.RunId;
        }

        RunListPage page = await runRepository.ListRecentInScopeKeysetAsync(scope, cursorUtc, cursorRunId, take, cancellationToken);
        IReadOnlyList<RunSummary> items = page.Items.Select(r => new RunSummary { RunId = r.RunId.ToString("N"), RequestId = r.ArchitectureRequestId ?? string.Empty, Status = r.LegacyRunStatus ?? nameof(ArchitectureRunStatus.Created), CreatedUtc = r.CreatedUtc, CompletedUtc = r.CompletedUtc, CurrentManifestVersion = r.CurrentManifestVersion, SystemName = r.ProjectId }).ToList();
        string? next = null;
        if (!page.HasMore || page.Items.Count <= 0)
            return (items, page.HasMore, next);
        RunRecord last = page.Items[^1];
        next = RunCursorCodec.Encode(last.CreatedUtc, last.RunId);
        return (items, page.HasMore, next);
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        return Guid.TryParseExact(runId, "N", out runGuid) || Guid.TryParse(runId, out runGuid);
    }
}
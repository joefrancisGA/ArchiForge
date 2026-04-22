using ArchLucid.Application.Runs.Mapping;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;

using DecisioningTraceRepository = ArchLucid.Decisioning.Interfaces.IDecisionTraceRepository;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application;

/// <summary>
/// Assembles the canonical <see cref="ArchitectureRunDetail"/> from individual repositories.
/// This is the single, authoritative query path for run state — controllers, API application
/// services, analysis/export/compare, governance, and <see cref="ReplayRunService"/> should use this
/// instead of assembling run metadata, tasks, results, manifest, and traces from repositories separately.
/// </summary>
public sealed class RunDetailQueryService(
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IUnifiedGoldenManifestReader unifiedGoldenManifestReader,
    ICoordinatorDecisionTraceRepository decisionTraceRepository,
    DecisioningTraceRepository authorityDecisionTraceRepository,
    ILogger<RunDetailQueryService> logger)
    : IRunDetailQueryService
{
    /// <inheritdoc />
    public async Task<ArchitectureRunDetail?> GetRunDetailAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (!TryParseRunGuid(runId, out Guid runGuid))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(
                    "RunDetailQueryService: run '{RunId}' is not a valid run identifier.",
                    LogSanitizer.Sanitize(runId));

            return null;
        }

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        Persistence.Models.RunRecord? record =
            await runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (record is null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(
                    "RunDetailQueryService: run '{RunId}' not found.",
                    LogSanitizer.Sanitize(runId));

            return null;
        }

        IReadOnlyList<AgentTask> tasks =
            await taskRepository.GetByRunIdAsync(runId, cancellationToken) ?? [];

        ArchitectureRun run = RunRecordToArchitectureRunMapper.ToArchitectureRun(
            record,
            tasks.Select(t => t.TaskId).ToList());

        IReadOnlyList<AgentResult> results =
            await resultRepository.GetByRunIdAsync(runId, cancellationToken) ?? [];

        Contracts.Manifest.GoldenManifest? manifest =
            await unifiedGoldenManifestReader.ReadByRunIdAsync(scope, runGuid, cancellationToken);

        List<DecisionTrace> decisionTraces = [];

        if (manifest is null)
        {
            if (!string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
                && logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(
                    "RunDetailQueryService: run '{RunId}' references manifest version '{Version}' which no longer exists.",
                    LogSanitizer.Sanitize(runId),
                    LogSanitizer.Sanitize(run.CurrentManifestVersion));
        }
        else
        {
            IReadOnlyList<DecisionTrace>? traces =
                await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

            decisionTraces = traces is null ? [] : traces.ToList();

            if (decisionTraces.Count == 0 && record.DecisionTraceId is { } authorityTraceId)
            {
                DecisionTrace? authorityTrace =
                    await authorityDecisionTraceRepository.GetByIdAsync(scope, authorityTraceId, cancellationToken);

                if (authorityTrace is not null)
                    decisionTraces = [authorityTrace];
            }
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunSummary>> ListRunSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        IReadOnlyList<Persistence.Models.RunRecord> records =
            await runRepository.ListRecentInScopeAsync(scope, 200, cancellationToken);

        return records
            .Select(r => new RunSummary
            {
                RunId = r.RunId.ToString("N"),
                RequestId = r.ArchitectureRequestId ?? string.Empty,
                Status = r.LegacyRunStatus ?? nameof(ArchitectureRunStatus.Created),
                CreatedUtc = r.CreatedUtc,
                CompletedUtc = r.CompletedUtc,
                CurrentManifestVersion = r.CurrentManifestVersion,
                SystemName = r.ProjectId
            })
            .ToList();
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
            return true;


        return Guid.TryParse(runId, out runGuid);
    }
}

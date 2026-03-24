using ArchiForge.Contracts.Architecture;
using ArchiForge.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application;

/// <summary>
/// Assembles the canonical <see cref="ArchitectureRunDetail"/> from individual repositories.
/// This is the single, authoritative query path for run state — controllers, API application
/// services, analysis/export/compare, governance, and <see cref="ReplayRunService"/> should use this
/// instead of assembling run metadata, tasks, results, manifest, and traces from repositories separately.
/// </summary>
public sealed class RunDetailQueryService(
    IArchitectureRunRepository runRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository decisionTraceRepository,
    ILogger<RunDetailQueryService> logger)
    : IRunDetailQueryService
{
    /// <inheritdoc />
    public async Task<ArchitectureRunDetail?> GetRunDetailAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        var run = await runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            logger.LogDebug("RunDetailQueryService: run '{RunId}' not found.", runId);
            return null;
        }

        var tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var results = await resultRepository.GetByRunIdAsync(runId, cancellationToken);

        Contracts.Manifest.GoldenManifest? manifest = null;
        var decisionTraces = new List<Contracts.Metadata.DecisionTrace>();

        if (!string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            manifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken);

            if (manifest is null)
            {
                logger.LogWarning(
                    "RunDetailQueryService: run '{RunId}' references manifest version '{Version}' which no longer exists.",
                    runId,
                    run.CurrentManifestVersion);
            }
            else
            {
                decisionTraces = (await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken)).ToList();
            }
        }

        return new ArchitectureRunDetail
        {
            Run = run,
            Tasks = tasks.ToList(),
            Results = results.ToList(),
            Manifest = manifest,
            DecisionTraces = decisionTraces
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunSummary>> ListRunSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await runRepository.ListAsync(cancellationToken);

        return items
            .Select(r => new RunSummary
            {
                RunId = r.RunId,
                RequestId = r.RequestId,
                Status = r.Status,
                CreatedUtc = r.CreatedUtc,
                CompletedUtc = r.CompletedUtc,
                CurrentManifestVersion = r.CurrentManifestVersion,
                SystemName = r.SystemName
            })
            .ToList();
    }
}

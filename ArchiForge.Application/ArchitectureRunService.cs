using System.Transactions;

using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application;

/// <summary>
/// Orchestrates the three-phase architecture run workflow: coordinate and persist a new run, execute simulated agents with evidence and evaluations, then resolve decisions and commit a <see cref="ArchiForge.Contracts.Manifest.GoldenManifest"/>.
/// </summary>
/// <remarks>
/// Dependencies include <see cref="ArchiForge.Coordinator.Services.ICoordinatorService"/>, repositories under <c>ArchiForge.Data.Repositories</c>, <see cref="ArchiForge.AgentSimulator.Services.IAgentExecutor"/>, <see cref="ArchiForge.Application.Evidence.IEvidenceBuilder"/>, and decision services from <c>ArchiForge.DecisionEngine</c>.
/// </remarks>
public sealed class ArchitectureRunService(
    ICoordinatorService coordinator,
    IAgentExecutor agentExecutor,
    IDecisionEngineService decisionEngine,
    IAgentEvaluationService agentEvaluationService,
    IAgentEvaluationRepository agentEvaluationRepository,
    IDecisionEngineV2 decisionEngineV2,
    IDecisionNodeRepository decisionNodeRepository,
    IEvidenceBuilder evidenceBuilder,
    IArchitectureRequestRepository requestRepository,
    IArchitectureRunRepository runRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IEvidenceBundleRepository evidenceBundleRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    ILogger<ArchitectureRunService> logger)
    : IArchitectureRunService
{
    public async Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        var coordination = await coordinator.CreateRunAsync(request, cancellationToken);

        if (!coordination.Success)
        {
            throw new InvalidOperationException(
                $"CreateRun failed: {string.Join("; ", coordination.Errors)}");
        }

        logger.LogInformation(
            "Creating architecture run: RunId={RunId}, RequestId={RequestId}, SystemName={SystemName}, Environment={Environment}",
            coordination.Run.RunId,
            request.RequestId,
            request.SystemName,
            request.Environment);

        using (var scope = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await requestRepository.CreateAsync(request, cancellationToken);
            await runRepository.CreateAsync(coordination.Run, cancellationToken);
            await evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);
            await taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);
            scope.Complete();
        }

        logger.LogInformation(
            "Architecture run created: RunId={RunId}, TaskCount={TaskCount}",
            coordination.Run.RunId,
            coordination.Tasks.Count);

        return new CreateRunResult
        {
            Run = coordination.Run,
            EvidenceBundle = coordination.EvidenceBundle,
            Tasks = coordination.Tasks
        };
    }

    /// <inheritdoc />
    public async Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Executing architecture run: RunId={RunId}",
            runId);

        var run = await runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        // Idempotency: ReadyForCommit and Committed are terminal for execution.
        // Always short-circuit; never re-run agents against a finished run.
        if (run.Status is ArchitectureRunStatus.ReadyForCommit or ArchitectureRunStatus.Committed)
        {
            var existingResults = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
            if (existingResults.Count > 0)
            {
                logger.LogInformation(
                    "ExecuteRunAsync is idempotent: returning existing results for RunId={RunId}, Status={Status}, ResultCount={ResultCount}",
                    runId,
                    run.Status,
                    existingResults.Count);

                return new ExecuteRunResult
                {
                    RunId = runId,
                    Results = existingResults.ToList()
                };
            }

            // Status says done but results are missing — inconsistent state. Refuse to re-execute
            // rather than producing duplicate inserts or overwriting committed data.
            throw new ConflictException(
                $"Run '{runId}' is in status '{run.Status}' but has no stored agent results. " +
                "The run is in an inconsistent state and cannot be safely re-executed.");
        }

        var request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        var tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException($"No tasks found for run '{runId}'.");
        }

        var evidence = await evidenceBuilder.BuildAsync(runId, request, cancellationToken);

        await agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken);

        var results = await agentExecutor.ExecuteAsync(
            runId,
            request,
            evidence,
            tasks,
            cancellationToken);

        await resultRepository.CreateManyAsync(results, cancellationToken);

        var evaluations = await agentEvaluationService.EvaluateAsync(
            runId,
            request,
            evidence,
            tasks,
            results,
            cancellationToken);

        await agentEvaluationRepository.CreateManyAsync(evaluations, cancellationToken);

        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: run.CurrentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Architecture run execution completed: RunId={RunId}, ResultCount={ResultCount}",
            runId,
            results.Count);

        return new ExecuteRunResult
        {
            RunId = runId,
            Results = results.ToList()
        };
    }

    public async Task<CommitRunResult> CommitRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Committing architecture run: RunId={RunId}",
            runId);

        var run = await runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new RunNotFoundException(runId);

        // Idempotency: if the run is already committed and has a manifest version,
        // return the existing manifest and decision traces instead of creating a new manifest version.
        if (run.Status is ArchitectureRunStatus.Committed &&
            !string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            var existingManifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken);
            if (existingManifest is not null)
            {
                var existingTraces = await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

                logger.LogInformation(
                    "CommitRunAsync is idempotent: returning existing manifest for RunId={RunId}, ManifestVersion={ManifestVersion}, TraceCount={TraceCount}",
                    runId,
                    run.CurrentManifestVersion,
                    existingTraces.Count);

                return new CommitRunResult
                {
                    Manifest = existingManifest,
                    DecisionTraces = existingTraces.ToList(),
                    Warnings = []
                };
            }
        }

        if (run.Status != ArchitectureRunStatus.ReadyForCommit)
        {
            if (run.Status == ArchitectureRunStatus.Failed)
            {
                throw new ConflictException($"Run '{runId}' is in Failed status and cannot be committed.");
            }

            if (run.Status == ArchitectureRunStatus.Committed)
            {
                throw new ConflictException(
                    $"Run '{runId}' is already committed but the manifest could not be loaded for idempotent replay.");
            }

            throw new ConflictException(
                $"Run '{runId}' cannot be committed in status '{run.Status}'. Execute the run until it reaches ReadyForCommit.");
        }

        var request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Evidence package for run '{runId}' was not found.");

        var tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var results = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (results.Count == 0)
        {
            throw new InvalidOperationException($"No agent results found for run '{runId}'.");
        }

        var evaluations = await agentEvaluationRepository.GetByRunIdAsync(runId, cancellationToken);
        var decisionNodes = await decisionEngineV2.ResolveAsync(
            runId,
            request,
            evidence,
            tasks,
            results,
            evaluations,
            cancellationToken);

        var manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? "v1"
            : IncrementManifestVersion(run.CurrentManifestVersion);

        var merge = decisionEngine.MergeResults(
            runId,
            request,
            manifestVersion,
            results,
            evaluations,
            decisionNodes,
            run.CurrentManifestVersion);

        if (!merge.Success)
        {
            await runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Failed,
                run.CurrentManifestVersion,
                DateTime.UtcNow,
                cancellationToken);

            throw new InvalidOperationException(
                $"CommitRun failed: {string.Join("; ", merge.Errors)}");
        }

        // Persist decision nodes, manifest, traces, and status atomically so a retry
        // cannot produce duplicate node rows from a previous partially-committed attempt.
        using (var scope = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken);
            await manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
            await decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);
            await runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Committed,
                merge.Manifest.Metadata.ManifestVersion,
                DateTime.UtcNow,
                cancellationToken);
            scope.Complete();
        }

        logger.LogInformation(
            "Architecture run committed: RunId={RunId}, ManifestVersion={ManifestVersion}, WarningCount={WarningCount}",
            runId,
            merge.Manifest.Metadata.ManifestVersion,
            merge.Warnings.Count);

        return new CommitRunResult
        {
            Manifest = merge.Manifest,
            DecisionTraces = merge.DecisionTraces,
            Warnings = merge.Warnings
        };
    }

    /// <summary>
    /// Parses a <c>vN</c> manifest version string and returns <c>v(N+1)</c>.
    /// Throws when <paramref name="currentVersion"/> is not in the expected <c>vN</c> format,
    /// preventing collisions from unrelated legacy or corrupted version strings all resolving to <c>v1</c>.
    /// </summary>
    private static string IncrementManifestVersion(string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "v1";

        if (currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(currentVersion[1..], out var versionNumber))
        {
            return $"v{versionNumber + 1}";
        }

        throw new InvalidOperationException(
            $"Cannot increment manifest version '{currentVersion}': expected 'vN' format (e.g. 'v1', 'v2'). " +
            "Verify the CurrentManifestVersion stored in the database has not been corrupted.");
    }
}

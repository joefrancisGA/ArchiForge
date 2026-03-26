using System.Transactions;

using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
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
        ArgumentNullException.ThrowIfNull(request);

        CoordinationResult coordination = await coordinator.CreateRunAsync(request, cancellationToken).ConfigureAwait(false);

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

        using (TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await requestRepository.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            await runRepository.CreateAsync(coordination.Run, cancellationToken).ConfigureAwait(false);
            await evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken).ConfigureAwait(false);
            await taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken).ConfigureAwait(false);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        logger.LogInformation(
            "Executing architecture run: RunId={RunId}",
            runId);

        ArchitectureRun run = await runRepository.GetByIdAsync(runId, cancellationToken).ConfigureAwait(false)
                              ?? throw new RunNotFoundException(runId);

        // Idempotency: ReadyForCommit and Committed are terminal for execution.
        // Always short-circuit; never re-run agents against a finished run.
        if (run.Status is ArchitectureRunStatus.ReadyForCommit or ArchitectureRunStatus.Committed)
        {
            IReadOnlyList<AgentResult> existingResults = await resultRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
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

        ArchitectureRequest request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken).ConfigureAwait(false)
                                      ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException($"No tasks found for run '{runId}'.");
        }

        AgentEvidencePackage evidence = await evidenceBuilder.BuildAsync(runId, request, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AgentResult> results = await agentExecutor.ExecuteAsync(
            runId,
            request,
            evidence,
            tasks,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AgentEvaluation> evaluations = await agentEvaluationService.EvaluateAsync(
            runId,
            request,
            evidence,
            tasks,
            results,
            cancellationToken).ConfigureAwait(false);

        // Persist all four writes atomically: a partial failure followed by a retry would
        // otherwise append duplicate result/evaluation rows because the repos are now
        // idempotent (delete-by-RunId + bulk insert) but only within a single transaction.
        using (TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken).ConfigureAwait(false);
            await resultRepository.CreateManyAsync(results, cancellationToken).ConfigureAwait(false);
            await agentEvaluationRepository.CreateManyAsync(evaluations, cancellationToken).ConfigureAwait(false);
            await runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.ReadyForCommit,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: null,
                cancellationToken: cancellationToken,
                expectedStatus: run.Status).ConfigureAwait(false);
            scope.Complete();
        }

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
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        logger.LogInformation(
            "Committing architecture run: RunId={RunId}",
            runId);

        ArchitectureRun run = await runRepository.GetByIdAsync(runId, cancellationToken).ConfigureAwait(false)
                              ?? throw new RunNotFoundException(runId);

        CommitRunResult? idempotent = await TryReturnCommittedManifestAsync(run, runId, cancellationToken).ConfigureAwait(false);
        if (idempotent is not null)
            return idempotent;

        EnforceCommitAllowedForStatus(run, runId);

        ArchitectureRequest request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken).ConfigureAwait(false)
                                      ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        await EnsureCommitPrerequisitesAsync(runId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AgentResult> results = await resultRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        if (results.Count == 0)
        {
            throw new InvalidOperationException($"No agent results found for run '{runId}'.");
        }

        IReadOnlyList<AgentEvaluation> evaluations = await agentEvaluationRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<DecisionNode> decisionNodes = await decisionEngineV2.ResolveAsync(
            runId,
            request,
            tasks,
            results,
            evaluations,
            cancellationToken).ConfigureAwait(false);

        string manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? "v1"
            : IncrementManifestVersion(run.CurrentManifestVersion);

        DecisionMergeResult merge = decisionEngine.MergeResults(
            runId,
            request,
            manifestVersion,
            results,
            evaluations,
            decisionNodes,
            run.CurrentManifestVersion);

        if (!merge.Success)
        {
            await FailRunAfterMergeFailureAsync(runId, run.CurrentManifestVersion, merge.Errors, cancellationToken).ConfigureAwait(false);
        }

        await PersistCommittedRunAsync(runId, decisionNodes, merge, cancellationToken).ConfigureAwait(false);

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
    /// When the run is already <see cref="ArchitectureRunStatus.Committed"/>, loads manifest and traces and returns a result, or throws if data is inconsistent.
    /// </summary>
    private async Task<CommitRunResult?> TryReturnCommittedManifestAsync(
        ArchitectureRun run,
        string runId,
        CancellationToken cancellationToken)
    {
        if (run.Status is not ArchitectureRunStatus.Committed)
            return null;

        if (string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            throw new ConflictException(
                $"Run '{runId}' is already committed but no manifest version was recorded. " +
                "The run record may be corrupt; check storage integrity.");
        }

        GoldenManifest? existingManifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken).ConfigureAwait(false);
        if (existingManifest is null)
        {
            throw new ConflictException(
                $"Run '{runId}' is already committed (manifest version '{run.CurrentManifestVersion}') " +
                "but the manifest could not be found in storage. " +
                "It may have been deleted or there is a replication lag.");
        }

        IReadOnlyList<DecisionTrace> existingTraces = await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);

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

    private static void EnforceCommitAllowedForStatus(ArchitectureRun run, string runId)
    {
        if (run.Status == ArchitectureRunStatus.ReadyForCommit)
            return;

        if (run.Status == ArchitectureRunStatus.Failed)
        {
            throw new ConflictException($"Run '{runId}' is in Failed status and cannot be committed.");
        }

        throw new ConflictException(
            $"Run '{runId}' cannot be committed in status '{run.Status}'. Execute the run until it reaches ReadyForCommit.");
    }

    private async Task EnsureCommitPrerequisitesAsync(string runId, CancellationToken cancellationToken)
    {
        _ = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Evidence package for run '{runId}' was not found.");
    }

    private async Task FailRunAfterMergeFailureAsync(
        string runId,
        string? currentManifestVersion,
        IReadOnlyList<string> mergeErrors,
        CancellationToken cancellationToken)
    {
        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Failed,
            currentManifestVersion,
            DateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);

        throw new InvalidOperationException(
            $"CommitRun failed: {string.Join("; ", mergeErrors)}");
    }

    private async Task PersistCommittedRunAsync(
        string runId,
        IReadOnlyList<DecisionNode> decisionNodes,
        DecisionMergeResult merge,
        CancellationToken cancellationToken)
    {
        using TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled);

        await decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken).ConfigureAwait(false);
        await manifestRepository.CreateAsync(merge.Manifest, cancellationToken).ConfigureAwait(false);
        await decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken).ConfigureAwait(false);
        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Committed,
            merge.Manifest.Metadata.ManifestVersion,
            DateTime.UtcNow,
            cancellationToken,
            expectedStatus: ArchitectureRunStatus.ReadyForCommit).ConfigureAwait(false);
        scope.Complete();
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
            int.TryParse(currentVersion[1..], out int versionNumber))
        {
            return $"v{versionNumber + 1}";
        }

        throw new InvalidOperationException(
            $"Cannot increment manifest version '{currentVersion}': expected 'vN' format (e.g. 'v1', 'v2'). " +
            "Verify the CurrentManifestVersion stored in the database has not been corrupted.");
    }
}

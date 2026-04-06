using System.Security.Cryptography;
using System.Transactions;

using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Common;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.Application.Runs;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.DecisionTraces;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Decisioning.Merge;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application;

/// <summary>
/// Orchestrates the three-phase architecture run workflow: coordinate and persist a new run, execute simulated agents with evidence and evaluations, then resolve decisions and commit a <see cref="ArchiForge.Contracts.Manifest.GoldenManifest"/>.
/// </summary>
/// <remarks>
/// Dependencies include <see cref="ArchiForge.Coordinator.Services.ICoordinatorService"/>, repositories under <c>ArchiForge.Persistence.Data.Repositories</c>, <see cref="ArchiForge.AgentSimulator.Services.IAgentExecutor"/>, <see cref="ArchiForge.Application.Evidence.IEvidenceBuilder"/>, manifest merge services under <c>ArchiForge.Decisioning.Merge</c>, and JSON schema validation under <c>ArchiForge.Decisioning.Validation</c>.
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
    ICoordinatorGoldenManifestRepository manifestRepository,
    IEvidenceBundleRepository evidenceBundleRepository,
    ICoordinatorDecisionTraceRepository decisionTraceRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IArchitectureRunIdempotencyRepository architectureRunIdempotencyRepository,
    IActorContext actorContext,
    IBaselineMutationAuditService baselineMutationAudit,
    ILogger<ArchitectureRunService> logger)
    : IArchitectureRunService
{
    /// <inheritdoc />
    public async Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string actor = actorContext.GetActor();

        if (idempotency is not null)
        {
            CreateRunResult? replay = await TryReplayFromIdempotencyAsync(idempotency, cancellationToken);
            if (replay is not null)
                return replay;
        }

        CoordinationResult coordination = await coordinator.CreateRunAsync(request, cancellationToken);

        if (!coordination.Success)
        {
            string detail = string.Join("; ", coordination.Errors);

            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    request.RequestId,
                    $"Coordination failed: {detail}",
                    cancellationToken)
                ;

            throw new InvalidOperationException(
                $"CreateRun failed: {detail}");
        }

        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Creating architecture run: RunId={RunId}, RequestId={RequestId}, SystemName={SystemName}, Environment={Environment}",
                coordination.Run.RunId,
                request.RequestId,
                request.SystemName,
                request.Environment);
        

        bool inserted;

        try
        {
            using TransactionScope scope = new(
                TransactionScopeOption.Required,
                TransactionScopeAsyncFlowOption.Enabled);
            inserted = await PersistCreateRunRowsAsync(
                request,
                coordination,
                idempotency,
                cancellationToken);

            if (inserted || idempotency is null)
                scope.Complete();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    coordination.Run.RunId,
                    $"Persist failed: {ex.GetType().Name}",
                    cancellationToken)
                ;

            throw;
        }

        if (idempotency is not null && !inserted)
        {
            CreateRunResult? winner = await ResolveIdempotencyRaceAsync(idempotency, cancellationToken);
            if (winner is not null)
                return winner;

            throw new InvalidOperationException(
                "Idempotency insert failed but no winning row was found; retry the request.");
        }

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunCreated,
                actor,
                coordination.Run.RunId,
                $"RequestId={request.RequestId}; Environment={request.Environment}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
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

    private async Task<bool> PersistCreateRunRowsAsync(
        ArchitectureRequest request,
        CoordinationResult coordination,
        CreateRunIdempotencyState? idempotency,
        CancellationToken cancellationToken)
    {
        await requestRepository.CreateAsync(request, cancellationToken);
        await runRepository.CreateAsync(coordination.Run, cancellationToken);
        await evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);

        if (coordination.Tasks.Count > 0)
            await taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);

        if (idempotency is null)
            return false;

        bool inserted = await architectureRunIdempotencyRepository
            .TryInsertAsync(
                idempotency.TenantId,
                idempotency.WorkspaceId,
                idempotency.ProjectId,
                idempotency.IdempotencyKeyHash,
                idempotency.RequestFingerprint,
                coordination.Run.RunId,
                cancellationToken)
            ;

        if (!inserted)
        
            logger.LogInformation(
                "Idempotency insert did not win race for RunId={RunId}; SQL Server rolls back the ambient transaction when the scope is not completed.",
                coordination.Run.RunId);
        

        return inserted;
    }

    private async Task<CreateRunResult?> TryReplayFromIdempotencyAsync(
        CreateRunIdempotencyState idempotency,
        CancellationToken cancellationToken)
    {
        ArchitectureRunIdempotencyLookup? existing = await architectureRunIdempotencyRepository
            .TryGetAsync(
                idempotency.TenantId,
                idempotency.WorkspaceId,
                idempotency.ProjectId,
                idempotency.IdempotencyKeyHash,
                cancellationToken)
            ;

        if (existing is null)
            return null;

        if (!CryptographicOperations.FixedTimeEquals(existing.RequestFingerprint, idempotency.RequestFingerprint))
        
            throw new ConflictException(
                "The Idempotency-Key was already used with a different request body.");
        

        return await RehydrateCreateRunResultAsync(existing.RunId, cancellationToken);
    }

    private async Task<CreateRunResult?> ResolveIdempotencyRaceAsync(
        CreateRunIdempotencyState idempotency,
        CancellationToken cancellationToken)
    {
        ArchitectureRunIdempotencyLookup? winner = await architectureRunIdempotencyRepository
            .TryGetAsync(
                idempotency.TenantId,
                idempotency.WorkspaceId,
                idempotency.ProjectId,
                idempotency.IdempotencyKeyHash,
                cancellationToken)
            ;

        if (winner is null)
            return null;

        if (!CryptographicOperations.FixedTimeEquals(winner.RequestFingerprint, idempotency.RequestFingerprint))
        
            throw new ConflictException(
                "The Idempotency-Key was already used with a different request body.");
        

        return await RehydrateCreateRunResultAsync(winner.RunId, cancellationToken);
    }

    private async Task<CreateRunResult> RehydrateCreateRunResultAsync(
        string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRun run = await runRepository.GetByIdAsync(runId, cancellationToken)
                              ?? throw new InvalidOperationException($"Run '{runId}' from idempotency store was not found.");

        IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        
            throw new InvalidOperationException($"Idempotent run '{runId}' has no tasks.");
        

        string? bundleRef = tasks[0].EvidenceBundleRef;
        if (string.IsNullOrWhiteSpace(bundleRef))
        
            throw new InvalidOperationException($"Idempotent run '{runId}' is missing EvidenceBundleRef on the first task.");
        

        EvidenceBundle bundle = await evidenceBundleRepository.GetByIdAsync(bundleRef, cancellationToken)
                                ?? throw new InvalidOperationException($"Evidence bundle '{bundleRef}' for idempotent run was not found.");

        logger.LogInformation("CreateRun idempotent replay: RunId={RunId}, TaskCount={TaskCount}", runId, tasks.Count);

        return new CreateRunResult
        {
            Run = run,
            EvidenceBundle = bundle,
            Tasks = tasks.ToList(),
            IdempotentReplay = true
        };
    }

    /// <inheritdoc />
    public async Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        string actor = actorContext.GetActor();

        try
        {
            return await ExecuteRunCoreAsync(runId, actor, cancellationToken);
        }
        catch (RunNotFoundException)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    "Run not found.",
                    cancellationToken)
                ;

            throw;
        }
    }

    private async Task<ExecuteRunResult> ExecuteRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Executing architecture run: RunId={RunId}",
                runId);

        ArchitectureRun run = await runRepository.GetByIdAsync(runId, cancellationToken)
                              ?? throw new RunNotFoundException(runId);

        ExecuteRunResult? idempotent = await TryReturnExistingExecuteResultsAsync(run, runId, cancellationToken);
        if (idempotent is not null)
            return idempotent;

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunStarted,
                actor,
                runId,
                null,
                cancellationToken)
            ;

        try
        {
            ArchitectureRequest request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
                                          ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

            IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
            if (tasks.Count == 0)
            
                throw new InvalidOperationException($"No tasks found for run '{runId}'.");
            

            AgentEvidencePackage evidence = await evidenceBuilder.BuildAsync(runId, request, cancellationToken);

            IReadOnlyList<AgentResult> results = await agentExecutor.ExecuteAsync(
                runId,
                request,
                evidence,
                tasks,
                cancellationToken);

            IReadOnlyList<AgentEvaluation> evaluations = await agentEvaluationService.EvaluateAsync(
                runId,
                request,
                evidence,
                tasks,
                results,
                cancellationToken);

            await PersistExecutePhaseAsync(
                runId,
                run.Status,
                run.CurrentManifestVersion,
                evidence,
                results,
                evaluations,
                cancellationToken);

            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunExecuteSucceeded,
                    actor,
                    runId,
                    $"ResultCount={results.Count}",
                    cancellationToken)
                ;

            if (logger.IsEnabled(LogLevel.Information))
            
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Architecture run execution failed: RunId={RunId}, ExceptionType={ExceptionType}",
                runId,
                ex.GetType().Name);

            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    ex.GetType().Name,
                    cancellationToken)
                ;

            throw;
        }
    }

    /// <summary>
    /// Idempotency: <see cref="ArchitectureRunStatus.ReadyForCommit"/> and <see cref="ArchitectureRunStatus.Committed"/> are terminal;
    /// returns stored results or throws when the run record contradicts stored agent outputs.
    /// </summary>
    private async Task<ExecuteRunResult?> TryReturnExistingExecuteResultsAsync(
        ArchitectureRun run,
        string runId,
        CancellationToken cancellationToken)
    {
        if (run.Status is not ArchitectureRunStatus.ReadyForCommit and not ArchitectureRunStatus.Committed)
            return null;

        IReadOnlyList<AgentResult> existingResults = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
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

        throw new ConflictException(
            $"Run '{runId}' is in status '{run.Status}' but has no stored agent results. " +
            "The run is in an inconsistent state and cannot be safely re-executed.");
    }

    /// <summary>
    /// Persists evidence, results, evaluations, and status inside one transaction so retries do not duplicate rows.
    /// </summary>
    private async Task PersistExecutePhaseAsync(
        string runId,
        ArchitectureRunStatus expectedStatus,
        string? currentManifestVersion,
        AgentEvidencePackage evidence,
        IReadOnlyList<AgentResult> results,
        IReadOnlyList<AgentEvaluation> evaluations,
        CancellationToken cancellationToken)
    {
        using TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled);
        await PersistExecutePhaseRowsAsync(
            runId,
            expectedStatus,
            currentManifestVersion,
            evidence,
            results,
            evaluations,
            cancellationToken);

        scope.Complete();
    }

    private async Task PersistExecutePhaseRowsAsync(
        string runId,
        ArchitectureRunStatus expectedStatus,
        string? currentManifestVersion,
        AgentEvidencePackage evidence,
        IReadOnlyList<AgentResult> results,
        IReadOnlyList<AgentEvaluation> evaluations,
        CancellationToken cancellationToken)
    {
        await agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken);
        await resultRepository.CreateManyAsync(results, cancellationToken);
        await agentEvaluationRepository.CreateManyAsync(evaluations, cancellationToken);
        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: currentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken,
            expectedStatus: expectedStatus);
    }

    /// <inheritdoc />
    public async Task<CommitRunResult> CommitRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        string actor = actorContext.GetActor();

        try
        {
            return await CommitRunCoreAsync(runId, actor, cancellationToken);
        }
        catch (RunNotFoundException)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    "Run not found.",
                    cancellationToken)
                ;

            throw;
        }
    }

    private async Task<CommitRunResult> CommitRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        
            logger.LogInformation(
                "Committing architecture run: RunId={RunId}",
                runId);
        

        ArchitectureRun run = await runRepository.GetByIdAsync(runId, cancellationToken)
                              ?? throw new RunNotFoundException(runId);

        CommitRunResult? idempotent = await TryReturnCommittedManifestAsync(run, runId, cancellationToken);
        if (idempotent is not null)
            return idempotent;

        try
        {
            EnforceCommitAllowedForStatus(run, runId);
        }
        catch (ConflictException ex)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Commit blocked: {ex.Message}",
                    cancellationToken)
                ;

            throw;
        }

        IReadOnlyList<DecisionNode> decisionNodes;
        DecisionMergeResult merge;

        try
        {
            ArchitectureRequest request = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
                                          ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

            await EnsureCommitPrerequisitesAsync(runId, cancellationToken);

            IReadOnlyList<AgentTask> tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);
            IReadOnlyList<AgentResult> results = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
            if (results.Count == 0)
            
                throw new InvalidOperationException($"No agent results found for run '{runId}'.");
            

            IReadOnlyList<AgentEvaluation> evaluations = await agentEvaluationRepository.GetByRunIdAsync(runId, cancellationToken);
            decisionNodes = await decisionEngineV2.ResolveAsync(
                runId,
                request,
                tasks,
                results,
                evaluations,
                cancellationToken);

            // ManifestVersion is the PK on GoldenManifestVersions (global, not per-run). A literal "v1" collides
            // when multiple runs commit in the same database (e.g. integration tests sharing one factory).
            string manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
                ? $"v1-{runId}"
                : IncrementManifestVersion(run.CurrentManifestVersion);

            merge = decisionEngine.MergeResults(
                runId,
                request,
                manifestVersion,
                results,
                evaluations,
                decisionNodes,
                run.CurrentManifestVersion);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    ex.GetType().Name,
                    cancellationToken)
                ;

            throw;
        }

        if (!merge.Success)
        
            await FailRunAfterMergeFailureAsync(runId, run.CurrentManifestVersion, merge.Errors, actor, cancellationToken);
        

        try
        {
            await PersistCommittedRunAsync(runId, decisionNodes, merge, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Persist failed: {ex.GetType().Name}",
                    cancellationToken)
                ;

            throw;
        }

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunCompleted,
                actor,
                runId,
                $"ManifestVersion={merge.Manifest.Metadata.ManifestVersion}; WarningCount={merge.Warnings.Count}",
                cancellationToken)
            ;

        if (logger.IsEnabled(LogLevel.Information))
        
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
        
            throw new ConflictException(
                $"Run '{runId}' is already committed but no manifest version was recorded. " +
                "The run record may be corrupt; check storage integrity.");
        

        GoldenManifest existingManifest = await manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken) ?? throw new ConflictException(
                $"Run '{runId}' is already committed (manifest version '{run.CurrentManifestVersion}') " +
                "but the manifest could not be found in storage. " +
                "It may have been deleted or there is a replication lag.");

        IReadOnlyList<DecisionTrace> existingTraces = await decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
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
        
            throw new ConflictException($"Run '{runId}' is in Failed status and cannot be committed.");
        

        throw new ConflictException(
            $"Run '{runId}' cannot be committed in status '{run.Status}'. Execute the run until it reaches ReadyForCommit.");
    }

    private async Task EnsureCommitPrerequisitesAsync(string runId, CancellationToken cancellationToken)
    {
        _ = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Evidence package for run '{runId}' was not found.");
    }

    private async Task FailRunAfterMergeFailureAsync(
        string runId,
        string? currentManifestVersion,
        IReadOnlyList<string> mergeErrors,
        string actor,
        CancellationToken cancellationToken)
    {
        string detail = string.Join("; ", mergeErrors);

        await baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunFailed,
                actor,
                runId,
                $"Merge failed: {detail}",
                cancellationToken)
            ;

        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Failed,
            currentManifestVersion,
            DateTime.UtcNow,
            cancellationToken);

        throw new InvalidOperationException(
            $"CommitRun failed: {detail}");
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
        await PersistCommittedRunRowsAsync(runId, decisionNodes, merge, cancellationToken);
        scope.Complete();
    }

    private async Task PersistCommittedRunRowsAsync(
        string runId,
        IReadOnlyList<DecisionNode> decisionNodes,
        DecisionMergeResult merge,
        CancellationToken cancellationToken)
    {
        await decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken);
        await manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
        await decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);
        await runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Committed,
            merge.Manifest.Metadata.ManifestVersion,
            DateTime.UtcNow,
            cancellationToken,
            expectedStatus: ArchitectureRunStatus.ReadyForCommit);
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
        
            return $"v{versionNumber + 1}";
        

        throw new InvalidOperationException(
            $"Cannot increment manifest version '{currentVersion}': expected 'vN' format (e.g. 'v1', 'v2'). " +
            "Verify the CurrentManifestVersion stored in the database has not been corrupted.");
    }
}

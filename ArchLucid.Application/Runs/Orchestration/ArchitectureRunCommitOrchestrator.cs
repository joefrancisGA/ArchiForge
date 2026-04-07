using System.Data;

using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Runs.Orchestration;

/// <inheritdoc cref="IArchitectureRunCommitOrchestrator"/>
public sealed class ArchitectureRunCommitOrchestrator(
    IArchitectureRunRepository runRepository,
    IArchitectureRequestRepository requestRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IAgentEvaluationRepository agentEvaluationRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IDecisionEngineService decisionEngine,
    IDecisionEngineV2 decisionEngineV2,
    IDecisionNodeRepository decisionNodeRepository,
    ICoordinatorGoldenManifestRepository manifestRepository,
    ICoordinatorDecisionTraceRepository decisionTraceRepository,
    IActorContext actorContext,
    IBaselineMutationAuditService baselineMutationAudit,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    ILogger<ArchitectureRunCommitOrchestrator> logger) : IArchitectureRunCommitOrchestrator
{
    private readonly IArchitectureRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly IArchitectureRequestRepository _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
    private readonly IAgentTaskRepository _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    private readonly IAgentResultRepository _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
    private readonly IAgentEvaluationRepository _agentEvaluationRepository = agentEvaluationRepository ?? throw new ArgumentNullException(nameof(agentEvaluationRepository));
    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository =
        agentEvidencePackageRepository ?? throw new ArgumentNullException(nameof(agentEvidencePackageRepository));
    private readonly IDecisionEngineService _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
    private readonly IDecisionEngineV2 _decisionEngineV2 = decisionEngineV2 ?? throw new ArgumentNullException(nameof(decisionEngineV2));
    private readonly IDecisionNodeRepository _decisionNodeRepository = decisionNodeRepository ?? throw new ArgumentNullException(nameof(decisionNodeRepository));
    private readonly ICoordinatorGoldenManifestRepository _manifestRepository = manifestRepository ?? throw new ArgumentNullException(nameof(manifestRepository));
    private readonly ICoordinatorDecisionTraceRepository _decisionTraceRepository = decisionTraceRepository ?? throw new ArgumentNullException(nameof(decisionTraceRepository));
    private readonly IActorContext _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
    private readonly IBaselineMutationAuditService _baselineMutationAudit = baselineMutationAudit ?? throw new ArgumentNullException(nameof(baselineMutationAudit));
    private readonly IArchLucidUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
    private readonly ILogger<ArchitectureRunCommitOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        string actor = _actorContext.GetActor();

        try
        {
            return await CommitRunCoreAsync(runId, actor, cancellationToken);
        }
        catch (RunNotFoundException)
        {
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    "Run not found.",
                    cancellationToken);

            throw;
        }
    }

    private async Task<CommitRunResult> CommitRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Committing architecture run: RunId={RunId}",
                runId);
        }

        ArchitectureRun run = await _runRepository.GetByIdAsync(runId, cancellationToken)
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
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Commit blocked: {ex.Message}",
                    cancellationToken);

            throw;
        }

        IReadOnlyList<DecisionNode> decisionNodes;
        DecisionMergeResult merge;

        try
        {
            ArchitectureRequest request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
                                          ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

            await EnsureCommitPrerequisitesAsync(runId, cancellationToken);

            IReadOnlyList<AgentTask> tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
            IReadOnlyList<AgentResult> results = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);

            if (results.Count == 0)
                throw new InvalidOperationException($"No agent results found for run '{runId}'.");

            IReadOnlyList<AgentEvaluation> evaluations = await _agentEvaluationRepository.GetByRunIdAsync(runId, cancellationToken);
            decisionNodes = await _decisionEngineV2.ResolveAsync(
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

            merge = _decisionEngine.MergeResults(
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
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    ex.GetType().Name,
                    cancellationToken);

            throw;
        }

        if (!merge.Success)
            await FailRunAfterMergeFailureAsync(runId, run.CurrentManifestVersion, merge.Errors, actor, cancellationToken);

        IReadOnlyList<string> traceabilityGaps = CommittedManifestTraceabilityRules.GetLinkageGaps(
            merge.Manifest,
            merge.DecisionTraces);

        if (traceabilityGaps.Count > 0)
            throw new InvalidOperationException(
                "Committed manifest traceability invariant failed: " + string.Join("; ", traceabilityGaps));

        try
        {
            await PersistCommittedRunAsync(runId, decisionNodes, merge, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Persist failed: {ex.GetType().Name}",
                    cancellationToken);

            throw;
        }

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunCompleted,
                actor,
                runId,
                $"ManifestVersion={merge.Manifest.Metadata.ManifestVersion}; WarningCount={merge.Warnings.Count}",
                cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Architecture run committed: RunId={RunId}, ManifestVersion={ManifestVersion}, WarningCount={WarningCount}",
                runId,
                merge.Manifest.Metadata.ManifestVersion,
                merge.Warnings.Count);
        }

        return new CommitRunResult
        {
            Manifest = merge.Manifest,
            DecisionTraces = merge.DecisionTraces,
            Warnings = merge.Warnings,
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

        GoldenManifest existingManifest = await _manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken) ?? throw new ConflictException(
                $"Run '{runId}' is already committed (manifest version '{run.CurrentManifestVersion}') " +
                "but the manifest could not be found in storage. " +
                "It may have been deleted or there is a replication lag.");

        IReadOnlyList<DecisionTrace> existingTraces = await _decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        IReadOnlyList<string> storedGaps = CommittedManifestTraceabilityRules.GetLinkageGaps(existingManifest, existingTraces);

        if (storedGaps.Count > 0)
        {
            _logger.LogWarning(
                "Committed run {RunId} has manifest/trace linkage gaps (data drift or legacy row): {Gaps}",
                runId,
                string.Join("; ", storedGaps));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "CommitRunAsync is idempotent: returning existing manifest for RunId={RunId}, ManifestVersion={ManifestVersion}, TraceCount={TraceCount}",
                runId,
                run.CurrentManifestVersion,
                existingTraces.Count);
        }

        return new CommitRunResult
        {
            Manifest = existingManifest,
            DecisionTraces = existingTraces.ToList(),
            Warnings = [],
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
        _ = await _agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken)
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

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Architecture.RunFailed,
                actor,
                runId,
                $"Merge failed: {detail}",
                cancellationToken);

        await _runRepository.UpdateStatusAsync(
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
        await using IArchLucidUnitOfWork uow = await _unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            await PersistCommittedRunRowsAsync(runId, decisionNodes, merge, uow, cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task PersistCommittedRunRowsAsync(
        string runId,
        IReadOnlyList<DecisionNode> decisionNodes,
        DecisionMergeResult merge,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        if (uow.SupportsExternalTransaction)
        {
            await _decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken, uow.Connection, uow.Transaction);
            await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken, uow.Connection, uow.Transaction);
            await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken, uow.Connection, uow.Transaction);
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Committed,
                merge.Manifest.Metadata.ManifestVersion,
                DateTime.UtcNow,
                cancellationToken,
                expectedStatus: ArchitectureRunStatus.ReadyForCommit,
                connection: uow.Connection,
                transaction: uow.Transaction);
        }
        else
        {
            await _decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken);
            await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
            await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Committed,
                merge.Manifest.Metadata.ManifestVersion,
                DateTime.UtcNow,
                cancellationToken,
                expectedStatus: ArchitectureRunStatus.ReadyForCommit);
        }
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

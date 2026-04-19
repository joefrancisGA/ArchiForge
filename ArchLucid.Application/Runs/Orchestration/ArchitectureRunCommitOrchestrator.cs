using System.Text.Json;

using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Runs.Orchestration;

/// <inheritdoc cref="IArchitectureRunCommitOrchestrator"/>
public sealed class ArchitectureRunCommitOrchestrator(
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
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
    IPreCommitGovernanceGate preCommitGovernanceGate,
    IOptions<PreCommitGovernanceGateOptions> preCommitGovernanceGateOptions,
    IAuditService auditService,
    ITrialFunnelCommitHook trialFunnelCommitHook,
    ILogger<ArchitectureRunCommitOrchestrator> logger) : IArchitectureRunCommitOrchestrator
{
    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

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
    private readonly IPreCommitGovernanceGate _preCommitGovernanceGate =
        preCommitGovernanceGate ?? throw new ArgumentNullException(nameof(preCommitGovernanceGate));

    private readonly IOptions<PreCommitGovernanceGateOptions> _preCommitGovernanceGateOptions =
        preCommitGovernanceGateOptions ?? throw new ArgumentNullException(nameof(preCommitGovernanceGateOptions));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ITrialFunnelCommitHook _trialFunnelCommitHook =
        trialFunnelCommitHook ?? throw new ArgumentNullException(nameof(trialFunnelCommitHook));

    private readonly ILogger<ArchitectureRunCommitOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private const int CommitRunTransientMaxAttempts = 5;

    /// <inheritdoc />
    public async Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        string actor = _actorContext.GetActor();

        for (int attempt = 1; attempt <= CommitRunTransientMaxAttempts; attempt++)
        {
            try
            {
                return await CommitRunCoreAsync(runId, actor, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RunNotFoundException)
            {
                await _baselineMutationAudit
                    .RecordAsync(
                        AuditEventTypes.Baseline.Architecture.RunFailed,
                        actor,
                        runId,
                        "Run not found.",
                        cancellationToken);

                await CoordinatorRunFailedDurableAudit.TryLogAsync(
                    _auditService,
                    _scopeContextProvider,
                    _logger,
                    actor,
                    runId,
                    "Run not found.",
                    cancellationToken);

                throw;
            }
            catch (Exception ex) when (SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(ex))
            {
                CommitRunResult? reconciled = await TryReconcileAfterConcurrentCommitAsync(runId, cancellationToken);

                if (reconciled is not null)
                    return reconciled;

                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "CommitRunAsync unique-key violation without reconcilable manifest (attempt {Attempt}/{Max}) for RunId={RunId}.",
                        attempt,
                        CommitRunTransientMaxAttempts,
                        LogSanitizer.Sanitize(runId));
                }

                if (attempt >= CommitRunTransientMaxAttempts)
                {
                    throw new ConflictException(
                        $"Commit for run '{runId}' raced with another commit. The manifest could not be loaded yet; retry the request.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
            catch (Exception ex) when (SqlTransientDetector.IsTransient(ex) && attempt < CommitRunTransientMaxAttempts)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "CommitRunAsync transient database error (attempt {Attempt}/{Max}) for RunId={RunId}; retrying.",
                        attempt,
                        CommitRunTransientMaxAttempts,
                        LogSanitizer.Sanitize(runId));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException("CommitRunAsync exhausted transient retries without returning.");
    }

    /// <summary>
    /// After a duplicate-key failure (parallel first commit), reload run state and return the winner's persisted manifest if present.
    /// </summary>
    private async Task<CommitRunResult?> TryReconcileAfterConcurrentCommitAsync(string runId, CancellationToken cancellationToken)
    {
        ArchitectureRun? runAgain = await ArchitectureRunAuthorityReader.TryGetArchitectureRunAsync(
            _runRepository,
            _scopeContextProvider,
            _taskRepository,
            runId,
            cancellationToken);

        if (runAgain is null)
            return null;

        CommitRunResult? committed = await TryReturnCommittedManifestAsync(runAgain, runId, cancellationToken);

        if (committed is not null)
            return committed;

        return await TryReturnPersistedCommitIfExistsAsync(runAgain, runId, cancellationToken);
    }

    private async Task<CommitRunResult> CommitRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            // Barrier is not tracked through params object?[] boxing on LogInformation; value is sanitized (docs/CODEQL_TRIAGE.md).
            _logger.LogInformation(
                "Committing architecture run: RunId={RunId}",
                LogSanitizer.Sanitize(runId)); // codeql[cs/log-forging]
        }

        ArchitectureRun? run = await ArchitectureRunAuthorityReader.TryGetArchitectureRunAsync(
            _runRepository,
            _scopeContextProvider,
            _taskRepository,
            runId,
            cancellationToken);

        if (run is null)
        {
            throw new RunNotFoundException(runId);
        }

        CommitRunResult? idempotent = await TryReturnCommittedManifestAsync(run, runId, cancellationToken);

        if (idempotent is not null)
            return idempotent;

        idempotent = await TryReturnPersistedCommitIfExistsAsync(run, runId, cancellationToken);

        if (idempotent is not null)
            return idempotent;

        await EvaluatePreCommitGovernanceGateOrThrowAsync(runId, actor, cancellationToken);

        try
        {
            EnforceCommitAllowedForStatus(run, runId);
        }
        catch (ConflictException ex)
        {
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Commit blocked: {ex.Message}",
                    cancellationToken);

            await CoordinatorRunFailedDurableAudit.TryLogAsync(
                _auditService,
                _scopeContextProvider,
                _logger,
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

            IReadOnlyList<AgentTask> tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
            IReadOnlyList<AgentResult> results =
                await _resultRepository.GetByRunIdAsync(runId, cancellationToken) ?? [];

            if (results.Count == 0)
            {
                throw new ConflictException(
                    $"No agent results found for run '{runId}'. Execute the run before committing.");
            }

            await EnsureCommitPrerequisitesAsync(runId, cancellationToken);

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
            string manifestVersion = BuildManifestVersionForCommit(run, runId);

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
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    runId,
                    ex.GetType().Name,
                    cancellationToken);

            await CoordinatorRunFailedDurableAudit.TryLogAsync(
                _auditService,
                _scopeContextProvider,
                _logger,
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
            await TryMarkRunHeaderCommittedAsync(runId, merge.Manifest.Metadata.ManifestVersion, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    runId,
                    $"Persist failed: {ex.GetType().Name}",
                    cancellationToken);

            await CoordinatorRunFailedDurableAudit.TryLogAsync(
                _auditService,
                _scopeContextProvider,
                _logger,
                actor,
                runId,
                $"Persist failed: {ex.GetType().Name}",
                cancellationToken);

            throw;
        }

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunCompleted,
                actor,
                runId,
                $"ManifestVersion={merge.Manifest.Metadata.ManifestVersion}; WarningCount={merge.Warnings.Count}",
                cancellationToken);

        ScopeContext commitScope = _scopeContextProvider.GetCurrentScope();
        Guid? commitRunGuid = Guid.TryParse(runId, out Guid ridCommit) ? ridCommit : null;

        try
        {
            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.CoordinatorRunCommitCompleted,
                    ActorUserId = actor,
                    ActorUserName = actor,
                    TenantId = commitScope.TenantId,
                    WorkspaceId = commitScope.WorkspaceId,
                    ProjectId = commitScope.ProjectId,
                    RunId = commitRunGuid,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        runId,
                        manifestVersion = merge.Manifest.Metadata.ManifestVersion,
                        systemName = merge.Manifest.SystemName,
                    }),
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Durable audit for CoordinatorRunCommitCompleted failed for RunId={RunId}",
                    LogSanitizer.Sanitize(runId));
            }
        }

        DateTimeOffset committedUtc = DateTimeOffset.UtcNow;

        await _trialFunnelCommitHook
            .OnTrialTenantManifestCommittedAsync(commitScope.TenantId, committedUtc, cancellationToken)
            .ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformationArchitectureRunCommitted(
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
            _logger.LogWarningWithTwoSanitizedUserStrings(
                "Committed run {RunId} has manifest/trace linkage gaps (data drift or legacy row): {Gaps}",
                runId,
                string.Join("; ", storedGaps));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformationCommitRunIdempotentReturn(
                runId,
                run.CurrentManifestVersion ?? string.Empty,
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

        // Execute orchestrator no longer promotes legacy status to ReadyForCommit (ADR-0012); agent outputs still gate commit below.
        if (run.Status == ArchitectureRunStatus.TasksGenerated)
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
                AuditEventTypes.Baseline.Architecture.RunFailed,
                actor,
                runId,
                $"Merge failed: {detail}",
                cancellationToken);

        await CoordinatorRunFailedDurableAudit.TryLogAsync(
            _auditService,
            _scopeContextProvider,
            _logger,
            actor,
            runId,
            $"Merge failed: {detail}",
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
        }
        else
        {
            await _decisionNodeRepository.CreateManyAsync(decisionNodes, cancellationToken);
            await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
            await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);
        }
    }

    /// <summary>
    /// Detects an already-persisted manifest at the version this run would use for a first commit attempt (idempotent retry).
    /// Ensures <see cref="RunRecord.LegacyRunStatus"/> is patched to <see cref="ArchitectureRunStatus.Committed"/> when lagging.
    /// </summary>
    private async Task<CommitRunResult?> TryReturnPersistedCommitIfExistsAsync(
        ArchitectureRun run,
        string runId,
        CancellationToken cancellationToken)
    {
        if (run.Status is not ArchitectureRunStatus.ReadyForCommit and not ArchitectureRunStatus.TasksGenerated)
            return null;

        string manifestVersion = BuildManifestVersionForCommit(run, runId);
        GoldenManifest? existingManifest = await _manifestRepository.GetByVersionAsync(manifestVersion, cancellationToken);

        if (existingManifest is null)
            return null;

        if (!string.Equals(existingManifest.RunId, runId, StringComparison.Ordinal))
            return null;

        IReadOnlyList<DecisionTrace> existingTraces = await _decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

        if (existingTraces.Count == 0)
            return null;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformationCommitRunIdempotentReturn(runId, manifestVersion, existingTraces.Count);
        }

        IReadOnlyList<string> storedGaps = CommittedManifestTraceabilityRules.GetLinkageGaps(existingManifest, existingTraces);

        if (storedGaps.Count > 0)
        {
            _logger.LogWarningWithTwoSanitizedUserStrings(
                "Committed run {RunId} has manifest/trace linkage gaps (data drift or legacy row): {Gaps}",
                runId,
                string.Join("; ", storedGaps));
        }

        string committedVersion = string.IsNullOrWhiteSpace(existingManifest.Metadata?.ManifestVersion)
            ? manifestVersion
            : existingManifest.Metadata.ManifestVersion;

        await TryMarkRunHeaderCommittedAsync(runId, committedVersion, cancellationToken);

        return new CommitRunResult
        {
            Manifest = existingManifest,
            DecisionTraces = existingTraces.ToList(),
            Warnings = [],
        };
    }

    /// <summary>
    /// Aligns <see cref="RunRecord.LegacyRunStatus"/> and <see cref="RunRecord.CurrentManifestVersion"/> with a successful
    /// coordinator commit so list APIs (<c>GET /v1/architecture/runs</c>) match run detail and operator expectations.
    /// </summary>
    private async Task TryMarkRunHeaderCommittedAsync(
        string runId,
        string manifestVersion,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(runId, "N", out Guid runGuid) && !Guid.TryParse(runId, out runGuid))
            return;

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? header = await _runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (header is null)
            return;

        header.LegacyRunStatus = ArchitectureRunStatus.Committed.ToString();

        if (!string.IsNullOrWhiteSpace(manifestVersion))
            header.CurrentManifestVersion = manifestVersion;

        if (header.CompletedUtc is null)
            header.CompletedUtc = DateTime.UtcNow;

        await _runRepository.UpdateAsync(header, cancellationToken);
    }

    private static string BuildManifestVersionForCommit(ArchitectureRun run, string runId) =>
        ManifestVersionIncrementRules.BuildManifestVersionForCommit(run, runId);

    private async Task EvaluatePreCommitGovernanceGateOrThrowAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!_preCommitGovernanceGateOptions.Value.PreCommitGateEnabled)
        {
            return;
        }

        PreCommitGateResult gateResult = await _preCommitGovernanceGate.EvaluateAsync(runId, cancellationToken);

        if (gateResult.WarnOnly)
        {
            await EmitPreCommitWarnedAuditAsync(gateResult, runId, actor, cancellationToken);
            return;
        }

        if (!gateResult.Blocked)
        {
            return;
        }

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;
        string dataJson = JsonSerializer.Serialize(
            new
            {
                reason = gateResult.Reason,
                blockingFindingIds = gateResult.BlockingFindingIds,
                policyPackId = gateResult.PolicyPackId,
                minimumBlockingSeverity = gateResult.MinimumBlockingSeverity?.ToString(),
            });

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.GovernancePreCommitBlocked,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = dataJson,
            },
            cancellationToken);

        throw new PreCommitGovernanceBlockedException(gateResult);
    }

    private async Task EmitPreCommitWarnedAuditAsync(
        PreCommitGateResult gateResult,
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;

        string dataJson = JsonSerializer.Serialize(
            new
            {
                reason = gateResult.Reason,
                warnings = gateResult.Warnings,
                blockingFindingIds = gateResult.BlockingFindingIds,
                policyPackId = gateResult.PolicyPackId,
                minimumBlockingSeverity = gateResult.MinimumBlockingSeverity?.ToString(),
            });

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.GovernancePreCommitWarned,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = dataJson,
            },
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "Pre-commit governance gate warned (not blocked): RunId={RunId}, Reason={Reason}",
                LogSanitizer.Sanitize(runId),
                LogSanitizer.Sanitize(gateResult.Reason ?? string.Empty));
        }
    }
}

using System.Text.Json;

using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Contracts.Agents;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Cm = ArchLucid.Contracts.Manifest;
using DecisioningIdTraceRepository = ArchLucid.Decisioning.Interfaces.IDecisionTraceRepository;
using DecisioningIGoldenManifestRepository = ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository;
using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Runs.Orchestration;

/// <inheritdoc cref="IArchitectureRunCommitOrchestrator" />
public sealed class AuthorityDrivenArchitectureRunCommitOrchestrator(
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    IAgentTaskRepository taskRepository,
    IArchitectureRequestRepository requestRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IAgentResultRepository agentResultRepository,
    IGraphSnapshotRepository graphSnapshotRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IDecisionEngine decisionEngine,
    DecisioningIdTraceRepository decisionTraceRepository,
    DecisioningIGoldenManifestRepository goldenManifestRepository,
    IManifestHashService manifestHashService,
    IAuthorityCommitProjectionBuilder projectionBuilder,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IPreCommitGovernanceGate preCommitGovernanceGate,
    IOptions<PreCommitGovernanceGateOptions> preCommitGovernanceGateOptions,
    IActorContext actorContext,
    IBaselineMutationAuditService baselineMutationAudit,
    IAuditService auditService,
    ITrialFunnelCommitHook trialFunnelCommitHook,
    IFirstSessionLifecycleHook firstSessionLifecycleHook,
    ILogger<AuthorityDrivenArchitectureRunCommitOrchestrator> logger) : IArchitectureRunCommitOrchestrator
{
    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly IScopeContextProvider _scopeContextProvider = scopeContextProvider
                                                                  ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IAgentTaskRepository _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    private readonly IArchitectureRequestRepository _requestRepository =
        requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));

    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository =
        agentEvidencePackageRepository ?? throw new ArgumentNullException(nameof(agentEvidencePackageRepository));

    private readonly IAgentResultRepository _agentResultRepository =
        agentResultRepository ?? throw new ArgumentNullException(nameof(agentResultRepository));

    private readonly IGraphSnapshotRepository _graphSnapshotRepository =
        graphSnapshotRepository ?? throw new ArgumentNullException(nameof(graphSnapshotRepository));

    private readonly IFindingsSnapshotRepository _findingsSnapshotRepository = findingsSnapshotRepository
        ?? throw new ArgumentNullException(nameof(findingsSnapshotRepository));

    private readonly IDecisionEngine _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));

    private readonly DecisioningIdTraceRepository _decisionTraceRepository =
        decisionTraceRepository ?? throw new ArgumentNullException(nameof(decisionTraceRepository));

    private readonly DecisioningIGoldenManifestRepository _goldenManifestRepository =
        goldenManifestRepository ?? throw new ArgumentNullException(nameof(goldenManifestRepository));

    private readonly IManifestHashService _manifestHashService =
        manifestHashService ?? throw new ArgumentNullException(nameof(manifestHashService));

    private readonly IAuthorityCommitProjectionBuilder _projectionBuilder =
        projectionBuilder ?? throw new ArgumentNullException(nameof(projectionBuilder));

    private readonly IArchLucidUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory
        ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));

    private readonly IPreCommitGovernanceGate _preCommitGovernanceGate = preCommitGovernanceGate
        ?? throw new ArgumentNullException(nameof(preCommitGovernanceGate));

    private readonly IOptions<PreCommitGovernanceGateOptions> _preCommitGovernanceGateOptions =
        preCommitGovernanceGateOptions ?? throw new ArgumentNullException(nameof(preCommitGovernanceGateOptions));

    private readonly IActorContext _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
    private readonly IBaselineMutationAuditService _baselineMutationAudit =
        baselineMutationAudit ?? throw new ArgumentNullException(nameof(baselineMutationAudit));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    private readonly ITrialFunnelCommitHook _trialFunnelCommitHook = trialFunnelCommitHook
        ?? throw new ArgumentNullException(nameof(trialFunnelCommitHook));

    private readonly IFirstSessionLifecycleHook _firstSessionLifecycleHook =
        firstSessionLifecycleHook ?? throw new ArgumentNullException(nameof(firstSessionLifecycleHook));

    private readonly ILogger<AuthorityDrivenArchitectureRunCommitOrchestrator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private const int CommitRunTransientMaxAttempts = 5;

    private const int CommitRunTransientBackoffMillisecondsPerAttempt = 25;

    /// <inheritdoc />
    public async Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        string actor = _actorContext.GetActor();

        for (int attempt = 1; attempt <= CommitRunTransientMaxAttempts; attempt++)

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

                throw;
            }
            catch (Exception ex) when (SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(ex))
            {
                CommitRunResult? reconciled = await TryReconcileAfterConcurrentCommitAsync(runId, cancellationToken);

                if (reconciled is not null)
                    return reconciled;

                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(
                        ex,
                        "CommitRunAsync (authority) unique-key violation without reconcilable manifest (attempt {Attempt}/{Max}) for RunId={RunId}.",
                        attempt,
                        CommitRunTransientMaxAttempts,
                        LogSanitizer.Sanitize(runId));

                if (attempt >= CommitRunTransientMaxAttempts)
                    throw new ConflictException(
                        $"Commit for run '{runId}' raced with another commit. The manifest could not be loaded yet; retry the request.");

                await Task.Delay(
                    TimeSpan.FromMilliseconds(CommitRunTransientBackoffMillisecondsPerAttempt * attempt),
                    cancellationToken);
            }
            catch (Exception ex) when (SqlTransientDetector.IsTransient(ex) && attempt < CommitRunTransientMaxAttempts)
            {
                if (_logger.IsEnabled(LogLevel.Warning))

                    _logger.LogWarning(
                        ex,
                        "CommitRunAsync (authority) transient database error (attempt {Attempt}/{Max}) for RunId={RunId}; retrying.",
                        attempt,
                        CommitRunTransientMaxAttempts,
                        LogSanitizer.Sanitize(runId));

                await Task.Delay(
                    TimeSpan.FromMilliseconds(CommitRunTransientBackoffMillisecondsPerAttempt * attempt),
                    cancellationToken);
            }

        throw new InvalidOperationException("CommitRunAsync (authority) exhausted transient retries without returning.");
    }

    private async Task<CommitRunResult?> TryReconcileAfterConcurrentCommitAsync(
        string runId,
        CancellationToken cancellationToken)
    {
        ArchitectureRun? runAgain = await ArchitectureRunAuthorityReader.TryGetArchitectureRunAsync(
            _runRepository,
            _scopeContextProvider,
            _taskRepository,
            runId,
            cancellationToken);

        if (runAgain is null)
            return null;

        return await TryReturnAuthorityCommittedIdempotentAsync(runAgain, runId, cancellationToken);
    }

    private async Task<CommitRunResult> CommitRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Committing architecture run (authority): RunId={RunId}",
                LogSanitizer.Sanitize(runId));

        if (!Guid.TryParseExact(runId, "N", out Guid runGuid) && !Guid.TryParse(runId, out runGuid))
            throw new RunNotFoundException(runId);

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? runRecord = await _runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (runRecord is null)
            throw new RunNotFoundException(runId);

        ArchitectureRun? run = await ArchitectureRunAuthorityReader.TryGetArchitectureRunAsync(
            _runRepository,
            _scopeContextProvider,
            _taskRepository,
            runId,
            cancellationToken);

        if (run is null)
            throw new RunNotFoundException(runId);

        CommitRunResult? idempotent = await TryReturnAuthorityCommittedIdempotentAsync(run, runId, cancellationToken);

        if (idempotent is not null)
            return idempotent;

        if (run.Status is ArchitectureRunStatus.Committed)
        {
            if (run.GoldenManifestId is not null)
                throw new InvalidOperationException(
                    $"Run '{runId}' is already Committed but the authority idempotent re-load failed. Check data integrity for GoldenManifest and DecisionTrace.");

            if (!string.IsNullOrEmpty(run.CurrentManifestVersion))
                throw new InvalidOperationException(
                    "This run was committed on the legacy coordinator path. " +
                    "Re-commit idempotency and reads require a consistent authority run record (GoldenManifestId / DecisionTraceId populated).");

            throw new ConflictException(
                $"Run '{runId}' is already Committed but the run record has no committed manifest version or authority identifiers.");
        }

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

            throw;
        }

        Dm.GoldenManifest manifestModel;
        DecisionTrace trace;
        Cm.GoldenManifest contract;

        try
        {
            ArchitectureRequest request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
                ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");
            await EnsureCommitPrerequisitesAsync(runId, cancellationToken);

            if (runRecord.ContextSnapshotId is not { } contextSnapshotId
                || runRecord.GraphSnapshotId is not { } graphId
                || runRecord.FindingsSnapshotId is not { } findingsId)
                throw new InvalidOperationException(
                    $"Run '{runId}' is missing authority pipeline snapshot ids (ContextSnapshotId, GraphSnapshotId, and FindingsSnapshotId are all required for authority commit).");

            GraphSnapshot? graph = await _graphSnapshotRepository.GetByIdAsync(graphId, cancellationToken);

            if (graph is null)
                throw new InvalidOperationException($"Graph snapshot '{graphId:D}' for run '{runId}' was not found.");

            IReadOnlyList<AgentResult> agentResults = await _agentResultRepository.GetByRunIdAsync(runId, cancellationToken);
            GraphSnapshot graphForDecision = AgentTopologyProposalGraphMerge.WithMergedTopologyProposals(graph, agentResults);

            Dm.FindingsSnapshot? findings = await _findingsSnapshotRepository.GetByIdAsync(findingsId, cancellationToken);

            if (findings is null)
                throw new InvalidOperationException($"Findings snapshot '{findingsId:D}' for run '{runId}' was not found.");


            (manifestModel, trace) = await _decisionEngine.DecideAsync(
                runGuid,
                contextSnapshotId,
                graphForDecision,
                findings,
                cancellationToken);

            ApplyRuleAuditScope(trace, scope);
            ApplyAuthorityManifestScope(manifestModel, scope);

            contract = await _projectionBuilder.BuildAsync(
                manifestModel,
                new()
                {
                    SystemName = request.SystemName
                },
                cancellationToken);

            AlignAuthorityVersionToContract(manifestModel, contract);

            IReadOnlyList<string> traceabilityGaps = AuthorityCommitTraceabilityRules.GetLinkageGaps(contract, [trace]);

            if (traceabilityGaps.Count > 0)
                throw new InvalidOperationException(
                    "Committed manifest traceability (authority) invariant failed: " + string.Join("; ", traceabilityGaps));
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

            throw;
        }

        Dm.GoldenManifest persisted;

        try
        {
            persisted = await PersistAuthorityAsync(manifestModel, contract, trace, cancellationToken);
            await TryMarkRunHeaderCommittedForAuthorityAsync(
                runId,
                contract.Metadata.ManifestVersion,
                persisted.ManifestId,
                trace.RequireRuleAudit().DecisionTraceId,
                cancellationToken);
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

            throw;
        }

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunCompleted,
                actor,
                runId,
                $"ManifestVersion={contract.Metadata.ManifestVersion}; SystemName={contract.SystemName}; WarningCount={persisted.Warnings.Count}; CommitPath=authority",
                cancellationToken);

        ScopeContext commitScope = _scopeContextProvider.GetCurrentScope();

        DateTimeOffset committedUtc = DateTimeOffset.UtcNow;
        // Pins dbo.Tenants.TrialFirstManifestCommittedUtc for every tenant on first commit; trial-funnel audit/metrics stay inside the hook.
        await _trialFunnelCommitHook.OnTrialTenantManifestCommittedAsync(commitScope.TenantId, committedUtc, cancellationToken);
        await _firstSessionLifecycleHook.OnSuccessfulManifestCommitAsync(commitScope.TenantId, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Architecture run committed (authority): RunId={RunId} ManifestVersion={Version} WarningCount={Wc}",
                LogSanitizer.Sanitize(runId),
                contract.Metadata.ManifestVersion,
                persisted.Warnings.Count);

        return new CommitRunResult
        {
            Manifest = contract,
            DecisionTraces = [trace],
            Warnings = persisted.Warnings.Count == 0 ? [] : [.. persisted.Warnings]
        };
    }

    private async Task<Dm.GoldenManifest> PersistAuthorityAsync(
        Dm.GoldenManifest manifestModel,
        Cm.GoldenManifest contract,
        DecisionTrace trace,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        SaveContractsManifestOptions keying = BuildSaveContractsManifestOptions(manifestModel, trace);

        await using IArchLucidUnitOfWork uow = await _unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            if (uow.SupportsExternalTransaction)
            {
                await _decisionTraceRepository.SaveAsync(
                    trace,
                    cancellationToken,
                    uow.Connection,
                    uow.Transaction);

                Dm.GoldenManifest persisted = await _goldenManifestRepository.SaveAsync(
                    contract,
                    scope,
                    keying,
                    _manifestHashService,
                    cancellationToken,
                    uow.Connection,
                    uow.Transaction,
                    authorityPersistBody: manifestModel);

                await uow.CommitAsync(cancellationToken);

                return persisted;
            }

            await _decisionTraceRepository.SaveAsync(trace, cancellationToken);
            Dm.GoldenManifest persistedNoTx = await _goldenManifestRepository.SaveAsync(
                contract,
                scope,
                keying,
                _manifestHashService,
                cancellationToken,
                authorityPersistBody: manifestModel);

            await uow.CommitAsync(cancellationToken);

            return persistedNoTx;
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static SaveContractsManifestOptions BuildSaveContractsManifestOptions(
        Dm.GoldenManifest manifestModel,
        DecisionTrace trace)
    {
        RuleAuditTracePayload audit = trace.RequireRuleAudit();

        return new SaveContractsManifestOptions
        {
            ManifestId = manifestModel.ManifestId,
            RunId = manifestModel.RunId,
            ContextSnapshotId = manifestModel.ContextSnapshotId,
            GraphSnapshotId = manifestModel.GraphSnapshotId,
            FindingsSnapshotId = manifestModel.FindingsSnapshotId,
            DecisionTraceId = audit.DecisionTraceId,
            RuleSetId = manifestModel.RuleSetId,
            RuleSetVersion = manifestModel.RuleSetVersion,
            RuleSetHash = manifestModel.RuleSetHash,
            CreatedUtc = manifestModel.CreatedUtc,
        };
    }

    private async Task<CommitRunResult?> TryReturnAuthorityCommittedIdempotentAsync(
        ArchitectureRun run,
        string runId,
        CancellationToken cancellationToken)
    {
        if (run.Status is not ArchitectureRunStatus.Committed)
            return null;

        if (run.GoldenManifestId is not { } goldenId)
            return null;

        if (run.DecisionTraceId is not { } traceId)
            throw new ConflictException(
                $"Run '{runId}' is already committed (authority) but DecisionTraceId is missing on the run record.");

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Dm.GoldenManifest? manifestModel = await _goldenManifestRepository.GetByIdAsync(scope, goldenId, cancellationToken);

        if (manifestModel is null)
            throw new ConflictException(
                $"Run '{runId}' is already committed but the golden manifest '{goldenId:D}' could not be loaded for idempotent replay.");

        DecisionTrace? trace = await _decisionTraceRepository.GetByIdAsync(scope, traceId, cancellationToken);

        if (trace is null)
            throw new ConflictException(
                $"Run '{runId}' is already committed but the decision trace '{traceId:D}' could not be loaded for idempotent replay.");

        ArchitectureRequest request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        Cm.GoldenManifest contract = await _projectionBuilder.BuildAsync(
            manifestModel,
            new()
            {
                SystemName = request.SystemName
            },
            cancellationToken);

        IReadOnlyList<string> storedGaps = AuthorityCommitTraceabilityRules.GetLinkageGaps(contract, [trace]);

        if (storedGaps.Count > 0)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(
                    "Committed run (authority) {RunId} has manifest/trace linkage gaps: {Gaps}",
                    LogSanitizer.Sanitize(runId),
                    string.Join("; ", storedGaps));
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Commit run idempotent return (authority): RunId={RunId} ManifestId={ManifestId} TraceId={TraceId}",
                LogSanitizer.Sanitize(runId),
                goldenId.ToString("D"),
                traceId.ToString("D"));

        return new CommitRunResult
        {
            Manifest = contract,
            DecisionTraces = [trace],
            Warnings = manifestModel.Warnings.Count == 0 ? [] : [.. manifestModel.Warnings]
        };
    }

    private async Task TryMarkRunHeaderCommittedForAuthorityAsync(
        string runId,
        string manifestVersion,
        Guid goldenManifestId,
        Guid decisionTraceId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(runId, "N", out Guid runGuid) && !Guid.TryParse(runId, out runGuid))
            return;

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? header = await _runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (header is null)
            return;

        header.LegacyRunStatus = nameof(ArchitectureRunStatus.Committed);

        if (!string.IsNullOrWhiteSpace(manifestVersion))
            header.CurrentManifestVersion = manifestVersion;

        header.GoldenManifestId = goldenManifestId;
        header.DecisionTraceId = decisionTraceId;

        header.CompletedUtc ??= DateTime.UtcNow;

        await _runRepository.UpdateAsync(header, cancellationToken);
    }

    private async Task EnsureCommitPrerequisitesAsync(string runId, CancellationToken cancellationToken)
    {
        // ADR 0030 PR A3 (2026-04-24): missing evidence package = run hasn't been executed yet,
        // which is a conflict with the current run state, not a malformed request → 409 (not 400).
        _ = await _agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken)
            ?? throw new ConflictException(
                $"Run '{runId}' cannot be committed: no evidence package exists. Execute the run first.");
    }

    private static void EnforceCommitAllowedForStatus(ArchitectureRun run, string runId)
    {
        if (run.Status == ArchitectureRunStatus.ReadyForCommit)
            return;

        if (run.Status == ArchitectureRunStatus.TasksGenerated)
            return;

        if (run.Status == ArchitectureRunStatus.Failed)
            throw new ConflictException($"Run '{runId}' is in Failed status and cannot be committed.");

        throw new ConflictException(
            $"Run '{runId}' cannot be committed in status '{run.Status}'. Execute the run until it reaches ReadyForCommit.");
    }

    private static void ApplyRuleAuditScope(DecisionTrace trace, ScopeContext scope)
    {
        RuleAuditTracePayload audit = trace.RequireRuleAudit();
        audit.TenantId = scope.TenantId;
        audit.WorkspaceId = scope.WorkspaceId;
        audit.ProjectId = scope.ProjectId;
    }

    private static void ApplyAuthorityManifestScope(Dm.GoldenManifest manifest, ScopeContext scope)
    {
        manifest.TenantId = scope.TenantId;
        manifest.WorkspaceId = scope.WorkspaceId;
        manifest.ProjectId = scope.ProjectId;
    }

    /// <summary>
    ///     ADR 0030 PR A3 (2026-04-24) — the authority engine stores <c>Metadata.Version</c> as a
    ///     bare semver (e.g. <c>1.0.0</c>), while the projection builder maps it into the contract
    ///     as a <c>v</c>-prefixed version (e.g. <c>v1.0.0</c>). The contract value is what the API
    ///     returns to clients and what subsequent <c>GET /v1/architecture/manifest/{manifestVersion}</c>
    ///     lookups compare against (ordinal exact match on <c>Metadata.Version</c> via
    ///     <c>GetByContractManifestVersionAsync</c>). Without this alignment the persisted row would
    ///     never match the version the client just received → 404. Copying the contract version onto
    ///     the authority row before persistence keeps the read path round-tripping.
    /// </summary>
    private static void AlignAuthorityVersionToContract(Dm.GoldenManifest manifestModel, Cm.GoldenManifest contract)
    {
        if (manifestModel is null)
            throw new ArgumentNullException(nameof(manifestModel));
        if (contract is null)
            throw new ArgumentNullException(nameof(contract));
        if (string.IsNullOrWhiteSpace(contract.Metadata.ManifestVersion))
            return;

        manifestModel.Metadata.Version = contract.Metadata.ManifestVersion;
    }

    private async Task EvaluatePreCommitGovernanceGateOrThrowAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!_preCommitGovernanceGateOptions.Value.PreCommitGateEnabled)
            return;

        PreCommitGateResult gateResult = await _preCommitGovernanceGate.EvaluateAsync(runId, cancellationToken);

        if (gateResult.WarnOnly)
        {
            await EmitPreCommitWarnedAuditAsync(gateResult, runId, actor, cancellationToken);
            return;
        }

        if (!gateResult.Blocked)
            return;

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;
        string dataJson = JsonSerializer.Serialize(
            new
            {
                reason = gateResult.Reason,
                blockingFindingIds = gateResult.BlockingFindingIds,
                policyPackId = gateResult.PolicyPackId,
                minimumBlockingSeverity = gateResult.MinimumBlockingSeverity?.ToString()
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
                DataJson = dataJson
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
                minimumBlockingSeverity = gateResult.MinimumBlockingSeverity?.ToString()
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
                DataJson = dataJson
            },
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Warning))
            _logger.LogWarning(
                "Pre-commit governance gate warned (not blocked) — authority path: RunId={RunId}, Reason={Reason}",
                LogSanitizer.Sanitize(runId),
                LogSanitizer.Sanitize(gateResult.Reason ?? string.Empty));
    }
}

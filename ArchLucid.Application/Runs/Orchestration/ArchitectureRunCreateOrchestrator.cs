using System.Security.Cryptography;
using System.Text.Json;

using ArchLucid.Application.Common;
using ArchLucid.Application.Runs.Coordination;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Runs.Orchestration;

/// <inheritdoc cref="IArchitectureRunCreateOrchestrator" />
/// <remarks>
///     When HTTP idempotency is used, concurrent requests for the same key are serialized in-process from the
///     first missed replay through coordination and persistence so only one authority run is created per key.
///     When SQL persistence is configured, <see cref="IDistributedCreateRunIdempotencyLock" /> uses SQL Server
///     <c>sp_getapplock</c> so concurrent replicas serialize the same idempotency key.
/// </remarks>
public sealed class ArchitectureRunCreateOrchestrator(
    IArchitectureRunAuthorityCoordination authorityCoordination,
    IArchitectureRequestRepository requestRepository,
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    IEvidenceBundleRepository evidenceBundleRepository,
    IAgentTaskRepository taskRepository,
    IArchitectureRunIdempotencyRepository architectureRunIdempotencyRepository,
    IActorContext actorContext,
    IBaselineMutationAuditService baselineMutationAudit,
    IAuditService auditService,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IUsageMeteringService usageMetering,
    IDistributedCreateRunIdempotencyLock distributedCreateRunIdempotencyLock,
    IOptions<ArchitectureRunCreateOptions> createRunOptions,
    TimeProvider timeProvider,
    IRequestContentSafetyPrecheck requestContentSafetyPrecheck,
    ILogger<ArchitectureRunCreateOrchestrator> logger) : IArchitectureRunCreateOrchestrator
{
    private static readonly RunCreateIdempotencyGateCache IdempotencyGates = new();

    private readonly IRequestContentSafetyPrecheck _requestContentSafetyPrecheck =
        requestContentSafetyPrecheck ?? throw new ArgumentNullException(nameof(requestContentSafetyPrecheck));

    private readonly IActorContext
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IArchitectureRunIdempotencyRepository _architectureRunIdempotencyRepository =
        architectureRunIdempotencyRepository ??
        throw new ArgumentNullException(nameof(architectureRunIdempotencyRepository));

    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IArchitectureRunAuthorityCoordination _authorityCoordination =
        authorityCoordination ?? throw new ArgumentNullException(nameof(authorityCoordination));

    private readonly IBaselineMutationAuditService _baselineMutationAudit =
        baselineMutationAudit ?? throw new ArgumentNullException(nameof(baselineMutationAudit));

    private readonly IDistributedCreateRunIdempotencyLock _distributedCreateRunIdempotencyLock =
        distributedCreateRunIdempotencyLock ??
        throw new ArgumentNullException(nameof(distributedCreateRunIdempotencyLock));

    private readonly int _distributedIdempotencyLockTimeoutMs = ClampDistributedLockTimeout(
        createRunOptions ?? throw new ArgumentNullException(nameof(createRunOptions)));

    private readonly IEvidenceBundleRepository _evidenceBundleRepository =
        evidenceBundleRepository ?? throw new ArgumentNullException(nameof(evidenceBundleRepository));

    private readonly ILogger<ArchitectureRunCreateOrchestrator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IArchitectureRequestRepository _requestRepository =
        requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IAgentTaskRepository _taskRepository =
        taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private readonly IArchLucidUnitOfWorkFactory _unitOfWorkFactory =
        unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));

    private readonly IUsageMeteringService _usageMetering =
        usageMetering ?? throw new ArgumentNullException(nameof(usageMetering));

    /// <inheritdoc />
    public async Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestContentSafetyResult safety =
            await _requestContentSafetyPrecheck.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);

        if (!safety.IsAllowed)
        {
            string actor = _actorContext.GetActor();

            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    request.RequestId,
                    $"Request content failed safety precheck: {string.Join("; ", safety.Reasons)}",
                    cancellationToken)
                .ConfigureAwait(false);

            throw new InvalidOperationException(string.Join("; ", safety.Reasons));
        }

        // ReSharper disable once InvertIf
        if (idempotency is not null)
        {
            CreateRunResult? replay = await TryReplayFromIdempotencyAsync(idempotency, cancellationToken);

            if (replay is not null)
                return replay;

            string gateKey = BuildIdempotencyGateKey(idempotency);

            await using IAsyncDisposable _ = await _distributedCreateRunIdempotencyLock
                .AcquireExclusiveSessionLockAsync(gateKey, _distributedIdempotencyLockTimeoutMs, cancellationToken)
                .ConfigureAwait(false);
            CreateRunResult? replayUnderDistributed =
                await TryReplayFromIdempotencyAsync(idempotency, cancellationToken);

            if (replayUnderDistributed is not null)
                return replayUnderDistributed;

            SemaphoreSlim gate = IdempotencyGates.GetOrAddGate(gateKey);

            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CreateRunResult? replayUnderLock = await TryReplayFromIdempotencyAsync(idempotency, cancellationToken);

                if (replayUnderLock is not null)
                    return replayUnderLock;

                return await CreateRunWithCoordinationAsync(request, idempotency, cancellationToken);
            }
            finally
            {
                gate.Release();
                IdempotencyGates.TryEvictAfterRelease(gateKey);
            }
        }

        return await CreateRunWithCoordinationAsync(request, idempotency, cancellationToken);
    }

    /// <summary>
    ///     <c>sp_getapplock</c> wait budget while another request holds the same idempotency key.
    ///     The lock spans coordinator + persistence; parallel bursts must not time out waiting for the first winner.
    /// </summary>
    private static int ClampDistributedLockTimeout(IOptions<ArchitectureRunCreateOptions> options)
    {
        int ms = options.Value.DistributedIdempotencyLockTimeoutMilliseconds;

        if (ms < 1_000)
            return 1_000;

        return ms > 600_000 ? 600_000 : ms;
    }

    private async Task<CreateRunResult> CreateRunWithCoordinationAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency,
        CancellationToken cancellationToken)
    {
        string actor = _actorContext.GetActor();

        CoordinationResult coordination = await _authorityCoordination.CreateRunAsync(request, cancellationToken);

        if (!coordination.Success)
        {
            string detail = string.Join("; ", coordination.Errors);

            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    request.RequestId,
                    $"Coordination failed: {detail}",
                    cancellationToken);

            throw new InvalidOperationException(
                $"CreateRun failed: {detail}");
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformationCreatingArchitectureRun(
                coordination.Run.RunId,
                request.RequestId,
                request.SystemName,
                request.Environment);

        bool inserted;

        try
        {
            await using IArchLucidUnitOfWork uow = await _unitOfWorkFactory.CreateAsync(cancellationToken);

            try
            {
                inserted = await PersistCreateRunRowsAsync(
                    request,
                    coordination,
                    idempotency,
                    uow,
                    cancellationToken);

                if (inserted || idempotency is null)
                    await uow.CommitAsync(cancellationToken);
            }
            catch
            {
                await uow.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
                    actor,
                    coordination.Run.RunId,
                    $"Persist failed: {ex.GetType().Name}",
                    cancellationToken);

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

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunCreated,
                actor,
                coordination.Run.RunId,
                $"RequestId={request.RequestId}; Environment={request.Environment}; SystemName={request.SystemName}",
                cancellationToken);

        ScopeContext scopeCtx = _scopeContextProvider.GetCurrentScope();

        if (!TryParseCoordinationRunGuid(coordination.Run.RunId, out Guid runGuid))
            runGuid = Guid.Empty;

        await DurableAuditLogRetry.TryLogAsync(
            async ct =>
            {
                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.RequestCreated,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scopeCtx.TenantId,
                        WorkspaceId = scopeCtx.WorkspaceId,
                        ProjectId = scopeCtx.ProjectId,
                        RunId = runGuid == Guid.Empty ? null : runGuid,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                requestId = request.RequestId,
                                runId = coordination.Run.RunId,
                                systemName = request.SystemName,
                                environment = request.Environment,
                                cloudProvider = request.CloudProvider.ToString()
                            },
                            AuditJsonSerializationOptions.Instance)
                    },
                    ct);
            },
            _logger,
            $"{AuditEventTypes.RequestCreated}:{LogSanitizer.Sanitize(coordination.Run.RunId)}",
            cancellationToken,
            auditEventTypeForMetrics: AuditEventTypes.RequestCreated);

        await DurableAuditLogRetry.TryLogAsync(
            async ct =>
            {
                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.RequestLocked,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scopeCtx.TenantId,
                        WorkspaceId = scopeCtx.WorkspaceId,
                        ProjectId = scopeCtx.ProjectId,
                        RunId = runGuid == Guid.Empty ? null : runGuid,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                requestId = request.RequestId,
                                runId = coordination.Run.RunId,
                                rationale =
                                    "Run persisted for this ArchitectureRequest — request is scoped as locked relative to drafts until terminal runs settle."
                            },
                            AuditJsonSerializationOptions.Instance)
                    },
                    ct);
            },
            _logger,
            $"{AuditEventTypes.RequestLocked}:{LogSanitizer.Sanitize(coordination.Run.RunId)}",
            cancellationToken,
            auditEventTypeForMetrics: AuditEventTypes.RequestLocked);

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Architecture run created: RunId={RunId}, TaskCount={TaskCount}",
                LogSanitizer.Sanitize(coordination.Run.RunId),
                coordination.Tasks.Count);

        await TryRecordArchitectureRunMeteringAsync(
            _scopeContextProvider.GetCurrentScope(),
            coordination.Run.RunId,
            cancellationToken);

        return new CreateRunResult
        {
            Run = coordination.Run, EvidenceBundle = coordination.EvidenceBundle, Tasks = coordination.Tasks
        };
    }

    private static bool TryParseCoordinationRunGuid(string runId, out Guid runGuid)
    {
        return Guid.TryParseExact(runId, "N", out runGuid) || Guid.TryParse(runId, out runGuid);
    }

    private static string BuildIdempotencyGateKey(CreateRunIdempotencyState idempotency)
    {
        ArgumentNullException.ThrowIfNull(idempotency);

        byte[] hash = idempotency.IdempotencyKeyHash;
        if (hash is null || hash.Length == 0)
            throw new ArgumentException("Idempotency key hash must be non-empty.", nameof(idempotency));

        return string.Concat(
            idempotency.TenantId.ToString("N"),
            "|",
            idempotency.WorkspaceId.ToString("N"),
            "|",
            idempotency.ProjectId.ToString("N"),
            "|",
            Convert.ToHexString(hash));
    }

    private async Task TryRecordArchitectureRunMeteringAsync(
        ScopeContext scope,
        string runId,
        CancellationToken cancellationToken)
    {
        if (scope.TenantId == Guid.Empty)
            return;

        try
        {
            await _usageMetering
                .RecordAsync(
                    new UsageEvent
                    {
                        TenantId = scope.TenantId,
                        WorkspaceId = scope.WorkspaceId,
                        ProjectId = scope.ProjectId,
                        Kind = UsageMeterKind.ArchitectureRun,
                        Quantity = 1,
                        RecordedUtc = _timeProvider.GetUtcNow(),
                        CorrelationId = runId
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    ex,
                    "Usage metering failed for architecture run (tenant {TenantId}).",
                    scope.TenantId);
        }
    }

    private async Task<bool> PersistCreateRunRowsAsync(
        ArchitectureRequest request,
        CoordinationResult coordination,
        CreateRunIdempotencyState? idempotency,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        if (uow.SupportsExternalTransaction)
        {
            await _requestRepository.CreateAsync(request, cancellationToken, uow.Connection, uow.Transaction);
            await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken, uow.Connection,
                uow.Transaction);

            if (coordination.Tasks.Count > 0)
                await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken, uow.Connection,
                    uow.Transaction);
        }
        else
        {
            await _requestRepository.CreateAsync(request, cancellationToken);
            await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);

            if (coordination.Tasks.Count > 0)
                await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);
        }

        if (idempotency is null)
            return false;

        bool inserted = uow.SupportsExternalTransaction
            ? await _architectureRunIdempotencyRepository
                .TryInsertAsync(
                    idempotency.TenantId,
                    idempotency.WorkspaceId,
                    idempotency.ProjectId,
                    idempotency.IdempotencyKeyHash,
                    idempotency.RequestFingerprint,
                    coordination.Run.RunId,
                    cancellationToken,
                    uow.Connection,
                    uow.Transaction)
            : await _architectureRunIdempotencyRepository
                .TryInsertAsync(
                    idempotency.TenantId,
                    idempotency.WorkspaceId,
                    idempotency.ProjectId,
                    idempotency.IdempotencyKeyHash,
                    idempotency.RequestFingerprint,
                    coordination.Run.RunId,
                    cancellationToken);

        if (inserted)
            return inserted;

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Idempotency insert did not win race for RunId={RunId}; unit of work will roll back when not committed.",
                LogSanitizer.Sanitize(coordination.Run.RunId));

        return inserted;
    }

    private async Task<CreateRunResult?> TryReplayFromIdempotencyAsync(
        CreateRunIdempotencyState idempotency,
        CancellationToken cancellationToken)
    {
        ArchitectureRunIdempotencyLookup? existing = await _architectureRunIdempotencyRepository
            .TryGetAsync(
                idempotency.TenantId,
                idempotency.WorkspaceId,
                idempotency.ProjectId,
                idempotency.IdempotencyKeyHash,
                cancellationToken);

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
        ArchitectureRunIdempotencyLookup? winner = await _architectureRunIdempotencyRepository
            .TryGetAsync(
                idempotency.TenantId,
                idempotency.WorkspaceId,
                idempotency.ProjectId,
                idempotency.IdempotencyKeyHash,
                cancellationToken);

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
        ArchitectureRun? run = await ArchitectureRunAuthorityReader.TryGetArchitectureRunAsync(
            _runRepository,
            _scopeContextProvider,
            _taskRepository,
            runId,
            cancellationToken);

        if (run is null)
            throw new InvalidOperationException($"Run '{runId}' from idempotency store was not found.");

        IReadOnlyList<AgentTask> tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);

        if (tasks.Count == 0)
            throw new InvalidOperationException($"Idempotent run '{runId}' has no tasks.");

        string? bundleRef = tasks[0].EvidenceBundleRef;

        if (string.IsNullOrWhiteSpace(bundleRef))
            throw new InvalidOperationException(
                $"Idempotent run '{runId}' is missing EvidenceBundleRef on the first task.");

        EvidenceBundle bundle = await _evidenceBundleRepository.GetByIdAsync(bundleRef, cancellationToken)
                                ?? throw new InvalidOperationException(
                                    $"Evidence bundle '{bundleRef}' for idempotent run was not found.");

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "CreateRun idempotent replay: RunId={RunId}, TaskCount={TaskCount}",
                LogSanitizer.Sanitize(runId),
                tasks.Count);

        return new CreateRunResult
        {
            Run = run, EvidenceBundle = bundle, Tasks = tasks.ToList(), IdempotentReplay = true
        };
    }
}

using System.Text.Json;

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Runs.Orchestration;

/// <inheritdoc cref="IArchitectureRunExecuteOrchestrator"/>
public sealed class ArchitectureRunExecuteOrchestrator(
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    IArchitectureRequestRepository requestRepository,
    IAgentTaskRepository taskRepository,
    IAgentExecutor agentExecutor,
    IAgentEvaluationService agentEvaluationService,
    IAgentResultRepository resultRepository,
    IAgentEvaluationRepository agentEvaluationRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IEvidenceBuilder evidenceBuilder,
    IActorContext actorContext,
    IBaselineMutationAuditService baselineMutationAudit,
    IAuditService auditService,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IAgentOutputTraceEvaluationHook outputTraceEvaluationHook,
    ILogger<ArchitectureRunExecuteOrchestrator> logger) : IArchitectureRunExecuteOrchestrator
{
    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IArchitectureRequestRepository _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
    private readonly IAgentTaskRepository _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    private readonly IAgentExecutor _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
    private readonly IAgentEvaluationService _agentEvaluationService = agentEvaluationService ?? throw new ArgumentNullException(nameof(agentEvaluationService));
    private readonly IAgentResultRepository _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
    private readonly IAgentEvaluationRepository _agentEvaluationRepository = agentEvaluationRepository ?? throw new ArgumentNullException(nameof(agentEvaluationRepository));
    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository =
        agentEvidencePackageRepository ?? throw new ArgumentNullException(nameof(agentEvidencePackageRepository));
    private readonly IEvidenceBuilder _evidenceBuilder = evidenceBuilder ?? throw new ArgumentNullException(nameof(evidenceBuilder));
    private readonly IActorContext _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
    private readonly IBaselineMutationAuditService _baselineMutationAudit = baselineMutationAudit ?? throw new ArgumentNullException(nameof(baselineMutationAudit));
    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    private readonly IArchLucidUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
    private readonly IAgentOutputTraceEvaluationHook _outputTraceEvaluationHook =
        outputTraceEvaluationHook ?? throw new ArgumentNullException(nameof(outputTraceEvaluationHook));
    private readonly ILogger<ArchitectureRunExecuteOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>One persisted result per required agent type (Topology, Cost, Compliance, Critic) before commit.</summary>
    private static readonly HashSet<AgentType> RequiredAgentTypesForCommit =
        [AgentType.Topology, AgentType.Cost, AgentType.Compliance, AgentType.Critic];

    /// <inheritdoc />
    public async Task<ExecuteRunResult> ExecuteRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        string actor = _actorContext.GetActor();

        try
        {
            return await ExecuteRunCoreAsync(runId, actor, cancellationToken);
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
    }

    private async Task<ExecuteRunResult> ExecuteRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Executing architecture run: RunId={RunId}",
                LogSanitizer.Sanitize(runId));
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

        ExecuteRunResult? idempotent = await TryReturnExistingExecuteResultsAsync(run, runId, cancellationToken);

        if (idempotent is not null)
            return idempotent;

        await _baselineMutationAudit
            .RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunStarted,
                actor,
                runId,
                null,
                cancellationToken);

        try
        {
            ScopeContext scope = _scopeContextProvider.GetCurrentScope();
            Guid? runGuid = Guid.TryParse(runId, out Guid ridStart) ? ridStart : null;

            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.CoordinatorRunExecuteStarted,
                    ActorUserId = actor,
                    ActorUserName = actor,
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    RunId = runGuid,
                    DataJson = JsonSerializer.Serialize(new { runId }),
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Durable audit for CoordinatorRunExecuteStarted failed for RunId={RunId}",
                    LogSanitizer.Sanitize(runId));
            }
        }

        try
        {
            ArchitectureRequest request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
                                          ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

            IReadOnlyList<AgentTask> tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);

            if (tasks.Count == 0)
                throw new InvalidOperationException($"No tasks found for run '{runId}'.");

            AgentEvidencePackage evidence = await _evidenceBuilder.BuildAsync(runId, request, cancellationToken);

            IReadOnlyList<AgentResult> results = await _agentExecutor.ExecuteAsync(
                runId,
                request,
                evidence,
                tasks,
                cancellationToken);

            IReadOnlyList<AgentEvaluation> evaluations = await _agentEvaluationService.EvaluateAsync(
                runId,
                request,
                evidence,
                tasks,
                results,
                cancellationToken);

            await PersistExecutePhaseAsync(
                runId,
                evidence,
                results,
                evaluations,
                cancellationToken);

            try
            {
                await _outputTraceEvaluationHook.AfterSuccessfulExecuteAsync(runId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "Agent output trace evaluation hook failed after successful execute for RunId={RunId}; run outcome unchanged.",
                        LogSanitizer.Sanitize(runId));
                }
            }

            await TryPromoteRunLegacyStatusIfAllResultsPresentAsync(runId, results, cancellationToken);

            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded,
                    actor,
                    runId,
                    $"ResultCount={results.Count}",
                    cancellationToken);

            try
            {
                ScopeContext scopeSucceeded = _scopeContextProvider.GetCurrentScope();
                Guid? runGuidSucceeded = Guid.TryParse(runId, out Guid ridSucceeded) ? ridSucceeded : null;

                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.CoordinatorRunExecuteSucceeded,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = scopeSucceeded.TenantId,
                        WorkspaceId = scopeSucceeded.WorkspaceId,
                        ProjectId = scopeSucceeded.ProjectId,
                        RunId = runGuidSucceeded,
                        DataJson = JsonSerializer.Serialize(new { runId, resultCount = results.Count }),
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "Durable audit for CoordinatorRunExecuteSucceeded failed for RunId={RunId}",
                        LogSanitizer.Sanitize(runId));
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Architecture run execution completed: RunId={RunId}, ResultCount={ResultCount}",
                    LogSanitizer.Sanitize(runId),
                    results.Count);
            }

            return new ExecuteRunResult
            {
                RunId = runId,
                Results = results.ToList(),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Architecture run execution failed: RunId={RunId}, ExceptionType={ExceptionType}",
                    LogSanitizer.Sanitize(runId),
                    ex.GetType().Name);
            }

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
        IReadOnlyList<AgentResult> existingResults =
            await _resultRepository.GetByRunIdAsync(runId, cancellationToken) ?? Array.Empty<AgentResult>();

        if (run.Status is ArchitectureRunStatus.ReadyForCommit or ArchitectureRunStatus.Committed)
        {
            if (existingResults.Count > 0)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "ExecuteRunAsync is idempotent: returning existing results for RunId={RunId}, Status={Status}, ResultCount={ResultCount}",
                        LogSanitizer.Sanitize(runId),
                        run.Status,
                        existingResults.Count);
                }

                return new ExecuteRunResult
                {
                    RunId = runId,
                    Results = existingResults.ToList(),
                };
            }

            throw new ConflictException(
                $"Run '{runId}' is in status '{run.Status}' but has no stored agent results. " +
                "The run is in an inconsistent state and cannot be safely re-executed.");
        }

        // Authority LegacyRunStatus may still read TasksGenerated while execute results already exist; idempotency uses stored results.
        if (run.Status == ArchitectureRunStatus.TasksGenerated && existingResults.Count > 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "ExecuteRunAsync is idempotent: returning existing results for RunId={RunId}, Status={Status}, ResultCount={ResultCount} (legacy status may lag)",
                    LogSanitizer.Sanitize(runId),
                    run.Status,
                    existingResults.Count);
            }

            await TryPromoteRunLegacyStatusIfAllResultsPresentAsync(runId, existingResults, cancellationToken);

            return new ExecuteRunResult
            {
                RunId = runId,
                Results = existingResults.ToList(),
            };
        }

        return null;
    }

    private static bool HasAllRequiredAgentTypesForCommit(IReadOnlyList<AgentResult> results)
    {
        if (results.Count != RequiredAgentTypesForCommit.Count)
            return false;

        foreach (AgentType required in RequiredAgentTypesForCommit)

            if (results.Count(r => r.AgentType == required) != 1)
                return false;


        return true;
    }

    /// <summary>
    /// ADR-0012: execute no longer wrote <c>LegacyRunStatus</c>; clients and UIs still expect <see cref="ArchitectureRunStatus.ReadyForCommit"/>
    /// once all required agent outputs exist (matches commit prerequisites and <c>IArchitectureRunService</c> contract).
    /// </summary>
    private async Task TryPromoteRunLegacyStatusIfAllResultsPresentAsync(
        string runId,
        IReadOnlyList<AgentResult> results,
        CancellationToken cancellationToken)
    {
        if (!HasAllRequiredAgentTypesForCommit(results))
            return;

        if (!TryParseRunGuid(runId, out Guid runGuid))
            return;

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? header = await _runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (header is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Execute: cannot promote run {RunId} — dbo.Runs header missing.",
                    LogSanitizer.Sanitize(runId));
            }

            return;
        }

        if (string.Equals(header.LegacyRunStatus, ArchitectureRunStatus.ReadyForCommit.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(header.LegacyRunStatus, ArchitectureRunStatus.Committed.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        header.LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString();
        await _runRepository.UpdateAsync(header, cancellationToken);
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
            return true;

        return Guid.TryParse(runId, out runGuid);
    }

    /// <summary>
    /// Persists evidence, results, and evaluations inside one transaction so retries do not duplicate rows.
    /// </summary>
    private async Task PersistExecutePhaseAsync(
        string runId,
        AgentEvidencePackage evidence,
        IReadOnlyList<AgentResult> results,
        IReadOnlyList<AgentEvaluation> evaluations,
        CancellationToken cancellationToken)
    {
        await using IArchLucidUnitOfWork uow = await _unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            await PersistExecutePhaseRowsAsync(
                runId,
                evidence,
                results,
                evaluations,
                uow,
                cancellationToken);

            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task PersistExecutePhaseRowsAsync(
        string runId,
        AgentEvidencePackage evidence,
        IReadOnlyList<AgentResult> results,
        IReadOnlyList<AgentEvaluation> evaluations,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        if (uow.SupportsExternalTransaction)
        {
            await _agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken, uow.Connection, uow.Transaction);
            await _resultRepository.CreateManyAsync(results, cancellationToken, uow.Connection, uow.Transaction);
            await _agentEvaluationRepository.CreateManyAsync(evaluations, cancellationToken, uow.Connection, uow.Transaction);
        }
        else
        {
            await _agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken);
            await _resultRepository.CreateManyAsync(results, cancellationToken);
            await _agentEvaluationRepository.CreateManyAsync(evaluations, cancellationToken);
        }
    }
}

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;

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
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
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
    private readonly IArchLucidUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
    private readonly ILogger<ArchitectureRunExecuteOrchestrator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

            throw;
        }
    }

    private async Task<ExecuteRunResult> ExecuteRunCoreAsync(
        string runId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Executing architecture run: RunId={RunId}",
                runId);

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

            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded,
                    actor,
                    runId,
                    $"ResultCount={results.Count}",
                    cancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Architecture run execution completed: RunId={RunId}, ResultCount={ResultCount}",
                    runId,
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
            _logger.LogWarning(
                ex,
                "Architecture run execution failed: RunId={RunId}, ExceptionType={ExceptionType}",
                runId,
                ex.GetType().Name);

            await _baselineMutationAudit
                .RecordAsync(
                    AuditEventTypes.Baseline.Architecture.RunFailed,
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
                _logger.LogInformation(
                    "ExecuteRunAsync is idempotent: returning existing results for RunId={RunId}, Status={Status}, ResultCount={ResultCount}",
                    runId,
                    run.Status,
                    existingResults.Count);

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
            _logger.LogInformation(
                "ExecuteRunAsync is idempotent: returning existing results for RunId={RunId}, Status={Status}, ResultCount={ResultCount} (legacy status may lag)",
                runId,
                run.Status,
                existingResults.Count);

            return new ExecuteRunResult
            {
                RunId = runId,
                Results = existingResults.ToList(),
            };
        }

        return null;
    }

    /// <summary>
    /// Persists evidence, results, evaluations, and status inside one transaction so retries do not duplicate rows.
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

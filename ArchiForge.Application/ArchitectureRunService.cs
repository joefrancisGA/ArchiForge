using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.AgentSimulator.Services;

namespace ArchiForge.Application;

public sealed class ArchitectureRunService : IArchitectureRunService
{
    private readonly ICoordinatorService _coordinator;
    private readonly IAgentExecutor _agentExecutor;
    private readonly IDecisionEngineService _decisionEngine;
    private readonly IArchitectureRequestRepository _requestRepository;
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IAgentTaskRepository _taskRepository;
    private readonly IAgentResultRepository _resultRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IEvidenceBundleRepository _evidenceBundleRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;

    public ArchitectureRunService(
        ICoordinatorService coordinator,
        IAgentExecutor agentExecutor,
        IDecisionEngineService decisionEngine,
        IArchitectureRequestRepository requestRepository,
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IGoldenManifestRepository manifestRepository,
        IEvidenceBundleRepository evidenceBundleRepository,
        IDecisionTraceRepository decisionTraceRepository)
    {
        _coordinator = coordinator;
        _agentExecutor = agentExecutor;
        _decisionEngine = decisionEngine;
        _requestRepository = requestRepository;
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _evidenceBundleRepository = evidenceBundleRepository;
        _decisionTraceRepository = decisionTraceRepository;
    }

    public async Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        var coordination = _coordinator.CreateRun(request);

        if (!coordination.Success)
        {
            throw new InvalidOperationException(
                $"CreateRun failed: {string.Join("; ", coordination.Errors)}");
        }

        await _requestRepository.CreateAsync(request, cancellationToken);
        await _runRepository.CreateAsync(coordination.Run, cancellationToken);
        await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);
        await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);

        return new CreateRunResult
        {
            Run = coordination.Run,
            EvidenceBundle = coordination.EvidenceBundle,
            Tasks = coordination.Tasks
        };
    }

    public async Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{runId}' not found.");

        var request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException($"No tasks found for run '{runId}'.");
        }

        var results = await _agentExecutor.ExecuteAsync(runId, request, tasks, cancellationToken);

        foreach (var result in results)
        {
            await _resultRepository.CreateAsync(result, cancellationToken);
        }

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: run.CurrentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken);

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
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{runId}' not found.");

        var request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        var results = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (results.Count == 0)
        {
            throw new InvalidOperationException($"No agent results found for run '{runId}'.");
        }

        var manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? "v1"
            : IncrementManifestVersion(run.CurrentManifestVersion);

        var merge = _decisionEngine.MergeResults(
            runId,
            request,
            manifestVersion,
            results,
            run.CurrentManifestVersion);

        if (!merge.Success)
        {
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Failed,
                run.CurrentManifestVersion,
                DateTime.UtcNow,
                cancellationToken);

            throw new InvalidOperationException(
                $"CommitRun failed: {string.Join("; ", merge.Errors)}");
        }

        await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
        await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Committed,
            merge.Manifest.Metadata.ManifestVersion,
            DateTime.UtcNow,
            cancellationToken);

        return new CommitRunResult
        {
            Manifest = merge.Manifest,
            DecisionTraces = merge.DecisionTraces,
            Warnings = merge.Warnings
        };
    }

    private static string IncrementManifestVersion(string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "v1";

        if (currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(currentVersion[1..], out var versionNumber))
        {
            return $"v{versionNumber + 1}";
        }

        return "v1";
    }
}

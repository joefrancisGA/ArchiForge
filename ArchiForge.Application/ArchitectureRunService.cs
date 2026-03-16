using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;
using ArchiForge.AgentSimulator.Services;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Application;

public sealed class ArchitectureRunService : IArchitectureRunService
{
    private readonly ICoordinatorService _coordinator;
    private readonly IAgentExecutor _agentExecutor;
    private readonly IDecisionEngineService _decisionEngine;
    private readonly IEvidenceBuilder _evidenceBuilder;
    private readonly IArchitectureRequestRepository _requestRepository;
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IAgentTaskRepository _taskRepository;
    private readonly IAgentResultRepository _resultRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IEvidenceBundleRepository _evidenceBundleRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IAgentEvidencePackageRepository _agentEvidencePackageRepository;
    private readonly ILogger<ArchitectureRunService> _logger;

    public ArchitectureRunService(
        ICoordinatorService coordinator,
        IAgentExecutor agentExecutor,
        IDecisionEngineService decisionEngine,
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
    {
        _coordinator = coordinator;
        _agentExecutor = agentExecutor;
        _decisionEngine = decisionEngine;
        _evidenceBuilder = evidenceBuilder;
        _requestRepository = requestRepository;
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _evidenceBundleRepository = evidenceBundleRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _agentEvidencePackageRepository = agentEvidencePackageRepository;
        _logger = logger;
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

        _logger.LogInformation(
            "Creating architecture run: RunId={RunId}, RequestId={RequestId}, SystemName={SystemName}, Environment={Environment}",
            coordination.Run.RunId,
            request.RequestId,
            request.SystemName,
            request.Environment);

        await _requestRepository.CreateAsync(request, cancellationToken);
        await _runRepository.CreateAsync(coordination.Run, cancellationToken);
        await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);
        await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);

        _logger.LogInformation(
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

    public async Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing architecture run: RunId={RunId}",
            runId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{runId}' not found.");

        // Idempotency: if the run has already been executed and is ready for commit or committed,
        // return the existing results instead of re-executing agents.
        if (run.Status is ArchitectureRunStatus.ReadyForCommit or ArchitectureRunStatus.Committed)
        {
            var existingResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
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
                    Results = existingResults.ToList()
                };
            }
        }

        var request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{run.RequestId}' not found.");

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException($"No tasks found for run '{runId}'.");
        }

        var evidence = await _evidenceBuilder.BuildAsync(runId, request, cancellationToken);

        await _agentEvidencePackageRepository.CreateAsync(evidence, cancellationToken);

        var results = await _agentExecutor.ExecuteAsync(
            runId,
            request,
            evidence,
            tasks,
            cancellationToken);

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

        _logger.LogInformation(
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
        _logger.LogInformation(
            "Committing architecture run: RunId={RunId}",
            runId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"Run '{runId}' not found.");

        // Idempotency: if the run is already committed and has a manifest version,
        // return the existing manifest and decision traces instead of creating a new manifest version.
        if (run.Status is ArchitectureRunStatus.Committed &&
            !string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
        {
            var existingManifest = await _manifestRepository.GetByVersionAsync(run.CurrentManifestVersion, cancellationToken);
            if (existingManifest is not null)
            {
                var existingTraces = await _decisionTraceRepository.GetByRunIdAsync(runId, cancellationToken);

                _logger.LogInformation(
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

        _logger.LogInformation(
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

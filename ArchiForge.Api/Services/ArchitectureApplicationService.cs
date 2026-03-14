using ArchiForge.Api.Diagnostics;
using ArchiForge.Api.Models;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

namespace ArchiForge.Api.Services;

public sealed class ArchitectureApplicationService : IArchitectureApplicationService
{
    private readonly ICoordinatorService _coordinatorService;
    private readonly IDecisionEngineService _decisionEngineService;
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IAgentTaskRepository _taskRepository;
    private readonly IAgentResultRepository _resultRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IEvidenceBundleRepository _evidenceBundleRepository;
    private readonly IDecisionTraceRepository _decisionTraceRepository;
    private readonly IArchitectureRequestRepository _requestRepository;

    public ArchitectureApplicationService(
        ICoordinatorService coordinatorService,
        IDecisionEngineService decisionEngineService,
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IGoldenManifestRepository manifestRepository,
        IEvidenceBundleRepository evidenceBundleRepository,
        IDecisionTraceRepository decisionTraceRepository,
        IArchitectureRequestRepository requestRepository)
    {
        _coordinatorService = coordinatorService;
        _decisionEngineService = decisionEngineService;
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _evidenceBundleRepository = evidenceBundleRepository;
        _decisionTraceRepository = decisionTraceRepository;
        _requestRepository = requestRepository;
    }

    public async Task<CreateRunResult> CreateRunAsync(ArchitectureRequest request, CancellationToken cancellationToken = default)
    {
        var coordination = _coordinatorService.CreateRun(request);
        if (!coordination.Success)
        {
            return new CreateRunResult(false, null, coordination.Errors);
        }

        await _requestRepository.CreateAsync(request, cancellationToken);
        await _runRepository.CreateAsync(coordination.Run, cancellationToken);
        await _evidenceBundleRepository.CreateAsync(coordination.EvidenceBundle, cancellationToken);
        await _taskRepository.CreateManyAsync(coordination.Tasks, cancellationToken);

        var response = new CreateArchitectureRunResponse
        {
            Run = coordination.Run,
            EvidenceBundle = coordination.EvidenceBundle,
            Tasks = coordination.Tasks
        };
        return new CreateRunResult(true, response, []);
    }

    public async Task<GetRunResult?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
            return null;

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var results = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        return new GetRunResult(run, tasks, results);
    }

    public async Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult result, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return new SubmitResultResult(false, null, $"Run '{runId}' was not found.");
        }

        if (!string.Equals(result.RunId, runId, StringComparison.OrdinalIgnoreCase))
        {
            return new SubmitResultResult(false, null,
                $"Result RunId '{result.RunId}' does not match route runId '{runId}'.");
        }

        await _resultRepository.CreateAsync(result, cancellationToken);

        var allResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        var newStatus = allResults.Count >= 3 && run.Status == ArchitectureRunStatus.TasksGenerated
            ? ArchitectureRunStatus.ReadyForCommit
            : ArchitectureRunStatus.WaitingForResults;

        await _runRepository.UpdateStatusAsync(
            runId,
            newStatus,
            currentManifestVersion: run.CurrentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken);

        return new SubmitResultResult(true, result.ResultId, null);
    }

    public async Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return new CommitRunResult(false, null, ["Run not found"], []);
        }

        var request = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (request is null)
        {
            return new CommitRunResult(false, null,
                [$"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found."], []);
        }

        var allResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (allResults.Count == 0)
        {
            return new CommitRunResult(false, null, ["Cannot commit run with no agent results."], []);
        }

        var manifestVersion = string.IsNullOrWhiteSpace(run.CurrentManifestVersion)
            ? "v1"
            : IncrementManifestVersion(run.CurrentManifestVersion);

        var merge = _decisionEngineService.MergeResults(
            runId,
            request,
            manifestVersion,
            allResults,
            parentManifestVersion: run.CurrentManifestVersion);

        if (!merge.Success)
        {
            await _runRepository.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Failed,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: DateTime.UtcNow,
                cancellationToken: cancellationToken);

            return new CommitRunResult(false, null, merge.Errors, merge.Warnings);
        }

        await _manifestRepository.CreateAsync(merge.Manifest, cancellationToken);
        await _decisionTraceRepository.CreateManyAsync(merge.DecisionTraces, cancellationToken);

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.Committed,
            currentManifestVersion: merge.Manifest.Metadata.ManifestVersion,
            completedUtc: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        var response = new CommitRunResponse
        {
            Manifest = merge.Manifest,
            DecisionTraces = merge.DecisionTraces,
            Warnings = merge.Warnings
        };
        return new CommitRunResult(true, response, [], merge.Warnings);
    }

    public async Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default)
    {
        return await _manifestRepository.GetByVersionAsync(version, cancellationToken);
    }

    public async Task<SeedFakeResultsResult?> SeedFakeResultsAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return new SeedFakeResultsResult(false, 0, $"Run '{runId}' was not found.");
        }

        var architectureRequest = await _requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (architectureRequest is null)
        {
            return new SeedFakeResultsResult(false, 0,
                $"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found.");
        }

        var tasks = await _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        if (tasks.Count == 0)
        {
            return new SeedFakeResultsResult(false, 0, "No tasks exist for this run.");
        }

        var fakeResults = FakeAgentResultFactory.CreateStarterResults(runId, tasks, architectureRequest);

        foreach (var result in fakeResults)
        {
            await _resultRepository.CreateAsync(result, cancellationToken);
        }

        await _runRepository.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: run.CurrentManifestVersion,
            completedUtc: null,
            cancellationToken: cancellationToken);

        return new SeedFakeResultsResult(true, fakeResults.Count, null);
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

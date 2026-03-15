using ArchiForge.Api.Diagnostics;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Api.Services;

public sealed class ArchitectureApplicationService : IArchitectureApplicationService
{
    private readonly IArchitectureRunRepository _runRepository;
    private readonly IAgentTaskRepository _taskRepository;
    private readonly IAgentResultRepository _resultRepository;
    private readonly IGoldenManifestRepository _manifestRepository;
    private readonly IArchitectureRequestRepository _requestRepository;
    private readonly ILogger<ArchitectureApplicationService> _logger;

    public ArchitectureApplicationService(
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IGoldenManifestRepository manifestRepository,
        IArchitectureRequestRepository requestRepository,
        ILogger<ArchitectureApplicationService> logger)
    {
        _runRepository = runRepository;
        _taskRepository = taskRepository;
        _resultRepository = resultRepository;
        _manifestRepository = manifestRepository;
        _requestRepository = requestRepository;
        _logger = logger;
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

        _logger.LogInformation("Agent result submitted: RunId={RunId}, ResultId={ResultId}, AgentType={AgentType}, NewStatus={NewStatus}",
            runId, result.ResultId, result.AgentType, newStatus);

        return new SubmitResultResult(true, result.ResultId, null);
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

        _logger.LogInformation("Fake results seeded: RunId={RunId}, ResultCount={ResultCount}", runId, fakeResults.Count);

        return new SeedFakeResultsResult(true, fakeResults.Count, null);
    }
}

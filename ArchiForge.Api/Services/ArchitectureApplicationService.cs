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
    /// <summary>Agent types that must each have exactly one result before a run can transition to ReadyForCommit.</summary>
    private static readonly HashSet<AgentType> RequiredAgentTypes = [AgentType.Topology, AgentType.Cost, AgentType.Compliance];

    /// <summary>Run statuses that allow submitting agent results.</summary>
    private static readonly HashSet<ArchitectureRunStatus> ResultSubmissionAllowedStatuses =
        [ArchitectureRunStatus.TasksGenerated, ArchitectureRunStatus.WaitingForResults];

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

        var tasksTask = _taskRepository.GetByRunIdAsync(runId, cancellationToken);
        var resultsTask = _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        await Task.WhenAll(tasksTask, resultsTask);
        var tasks = await tasksTask;
        var results = await resultsTask;
        return new GetRunResult(run, tasks, results);
    }

    public async Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult result, CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return new SubmitResultResult(false, null, $"Run '{runId}' was not found.");
        }

        if (!ResultSubmissionAllowedStatuses.Contains(run.Status))
        {
            return new SubmitResultResult(false, null,
                $"Run is in status '{run.Status}' and does not accept agent results. Only TasksGenerated or WaitingForResults runs can receive results.");
        }

        if (!string.Equals(result.RunId, runId, StringComparison.OrdinalIgnoreCase))
        {
            return new SubmitResultResult(false, null,
                $"Result RunId '{result.RunId}' does not match route runId '{runId}'.");
        }

        var existingResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (existingResults.Any(r => string.Equals(r.TaskId, result.TaskId, StringComparison.Ordinal)))
        {
            return new SubmitResultResult(false, null,
                $"A result for task '{result.TaskId}' has already been submitted for this run.");
        }

        await _resultRepository.CreateAsync(result, cancellationToken);

        var allResults = existingResults.Append(result).ToList();
        var hasAllRequiredAgentTypes = HasAllRequiredAgentTypes(allResults);
        var newStatus = hasAllRequiredAgentTypes && run.Status == ArchitectureRunStatus.TasksGenerated
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

    private static bool HasAllRequiredAgentTypes(IReadOnlyList<AgentResult> results)
    {
        var agentTypesPresent = results.Select(r => r.AgentType).ToHashSet();
        return RequiredAgentTypes.IsSubsetOf(agentTypesPresent)
            && agentTypesPresent.IsSubsetOf(RequiredAgentTypes);
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

        var existingResults = await _resultRepository.GetByRunIdAsync(runId, cancellationToken);
        if (existingResults.Count > 0)
        {
            _logger.LogInformation("Fake results skipped (run already has results): RunId={RunId}, ExistingCount={Count}", runId, existingResults.Count);
            return new SeedFakeResultsResult(true, 0, null);
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

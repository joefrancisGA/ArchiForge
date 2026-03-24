using System.Transactions;

using ArchiForge.Api.Diagnostics;
using ArchiForge.Application;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Api.Services;

public sealed class ArchitectureApplicationService(
    IRunDetailQueryService runDetailQueryService,
    IArchitectureRunRepository runRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IArchitectureRequestRepository requestRepository,
    ILogger<ArchitectureApplicationService> logger)
    : IArchitectureApplicationService
{
    /// <summary>Agent types that must each have exactly one result before a run can transition to ReadyForCommit.</summary>
    private static readonly HashSet<AgentType> RequiredAgentTypes = [AgentType.Topology, AgentType.Cost, AgentType.Compliance, AgentType.Critic];

    /// <summary>Run statuses that allow submitting agent results.</summary>
    private static readonly HashSet<ArchitectureRunStatus> ResultSubmissionAllowedStatuses =
        [ArchitectureRunStatus.TasksGenerated, ArchitectureRunStatus.WaitingForResults];

    public async Task<GetRunResult?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return null;

        var detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
            return null;

        return new GetRunResult(detail.Run, detail.Tasks, detail.Results);
    }

    public async Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult? result, CancellationToken cancellationToken = default)
    {
        if (result is null)
            return new SubmitResultResult(false, null, "Agent result is required.", ApplicationServiceFailureKind.BadRequest);

        if (string.IsNullOrWhiteSpace(runId))
            return new SubmitResultResult(false, null, "RunId is required.", ApplicationServiceFailureKind.BadRequest);

        var detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
        {
            return new SubmitResultResult(false, null, $"Run '{runId}' was not found.", ApplicationServiceFailureKind.RunNotFound);
        }

        var run = detail.Run;
        var tasks = detail.Tasks;
        var existingResults = detail.Results;

        if (!ResultSubmissionAllowedStatuses.Contains(run.Status))
        {
            var allowed = string.Join(" or ", ResultSubmissionAllowedStatuses.OrderBy(s => s.ToString()));
            return new SubmitResultResult(false, null,
                $"Run is in status '{run.Status}' and does not accept agent results. Only {allowed} runs can receive results.",
                ApplicationServiceFailureKind.BadRequest);
        }

        if (!string.Equals(result.RunId, runId, StringComparison.OrdinalIgnoreCase))
        {
            return new SubmitResultResult(false, null,
                $"Result RunId '{result.RunId}' does not match route runId '{runId}'.",
                ApplicationServiceFailureKind.BadRequest);
        }

        var task = tasks.FirstOrDefault(t => string.Equals(t.TaskId, result.TaskId, StringComparison.Ordinal));
        if (task is null)
        {
            return new SubmitResultResult(false, null,
                $"Task '{result.TaskId}' was not found for run '{runId}'.",
                ApplicationServiceFailureKind.ResourceNotFound);
        }

        if (task.AgentType != result.AgentType)
        {
            return new SubmitResultResult(false, null,
                $"Result AgentType '{result.AgentType}' does not match task AgentType '{task.AgentType}' for task '{result.TaskId}'.",
                ApplicationServiceFailureKind.BadRequest);
        }

        if (existingResults.Any(r => string.Equals(r.TaskId, result.TaskId, StringComparison.Ordinal)))
        {
            return new SubmitResultResult(false, null,
                $"A result for task '{result.TaskId}' has already been submitted for this run.",
                ApplicationServiceFailureKind.BadRequest);
        }

        ArchitectureRunStatus newStatus;
        using (var tx = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await resultRepository.CreateAsync(result, cancellationToken);

            // Re-fetch results after insert so concurrent submissions see the full set and only one transition sets ReadyForCommit.
            var allResults = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
            var hasAllRequiredAgentTypes = HasAllRequiredAgentTypes(allResults);
            newStatus = hasAllRequiredAgentTypes
                ? ArchitectureRunStatus.ReadyForCommit
                : ArchitectureRunStatus.WaitingForResults;

            if (newStatus != run.Status)
            {
                await runRepository.UpdateStatusAsync(
                    runId,
                    newStatus,
                    currentManifestVersion: run.CurrentManifestVersion,
                    completedUtc: null,
                    cancellationToken: cancellationToken);
            }

            tx.Complete();
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Agent result submitted: RunId={RunId}, ResultId={ResultId}, AgentType={AgentType}, NewStatus={NewStatus}",
                runId, result.ResultId, result.AgentType, newStatus);

        return new SubmitResultResult(true, result.ResultId, null);
    }

    /// <summary>True when there is exactly one result for each required agent type and no extra types.</summary>
    private static bool HasAllRequiredAgentTypes(IReadOnlyList<AgentResult> results)
    {
        if (results.Count != RequiredAgentTypes.Count)
            return false;
        foreach (var required in RequiredAgentTypes)
        {
            if (results.Count(r => r.AgentType == required) != 1)
                return false;
        }
        return true;
    }

    public async Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default)
    {
        return await manifestRepository.GetByVersionAsync(version, cancellationToken);
    }

    public async Task<SeedFakeResultsResult> SeedFakeResultsAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return new SeedFakeResultsResult(false, 0, "RunId is required.", ApplicationServiceFailureKind.BadRequest);

        var detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
        {
            return new SeedFakeResultsResult(false, 0, $"Run '{runId}' was not found.", ApplicationServiceFailureKind.RunNotFound);
        }

        var run = detail.Run;

        if (!ResultSubmissionAllowedStatuses.Contains(run.Status))
        {
            var allowed = string.Join(" or ", ResultSubmissionAllowedStatuses.OrderBy(s => s.ToString()));
            return new SeedFakeResultsResult(false, 0,
                $"Run is in status '{run.Status}' and does not accept results. Only {allowed} runs can be seeded.",
                ApplicationServiceFailureKind.BadRequest);
        }

        var architectureRequest = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (architectureRequest is null)
        {
            return new SeedFakeResultsResult(false, 0,
                $"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found.",
                ApplicationServiceFailureKind.ResourceNotFound);
        }

        var tasks = detail.Tasks;

        if (tasks.Count == 0)
        {
            return new SeedFakeResultsResult(false, 0, "No tasks exist for this run.", ApplicationServiceFailureKind.BadRequest);
        }

        var existingResults = detail.Results;

        if (existingResults.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Fake results skipped (run already has results): RunId={RunId}, ExistingCount={Count}", runId, existingResults.Count);
            return new SeedFakeResultsResult(true, 0, null);
        }

        var fakeResults = FakeAgentResultFactory.CreateStarterResults(runId, tasks, architectureRequest);

        var newStatus = HasAllRequiredAgentTypes(fakeResults)
            ? ArchitectureRunStatus.ReadyForCommit
            : ArchitectureRunStatus.WaitingForResults;

        using (var scope = new TransactionScope(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled))
        {
            await resultRepository.CreateManyAsync(fakeResults, cancellationToken);
            await runRepository.UpdateStatusAsync(
                runId,
                newStatus,
                currentManifestVersion: run.CurrentManifestVersion,
                completedUtc: null,
                cancellationToken: cancellationToken);
            scope.Complete();
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Fake results seeded: RunId={RunId}, ResultCount={ResultCount}, NewStatus={NewStatus}", runId, fakeResults.Count, newStatus);

        return new SeedFakeResultsResult(true, fakeResults.Count, null);
    }
}

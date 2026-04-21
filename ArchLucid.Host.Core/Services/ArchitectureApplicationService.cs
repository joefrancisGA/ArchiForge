using ArchLucid.Application;
using ArchLucid.Application.Evidence;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Host.Core.Diagnostics;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Host.Core.Services;

/// <summary>
/// API-facing orchestration service that coordinates run retrieval, agent result submission,
/// manifest access, and fake-result seeding for the architecture run lifecycle.
/// </summary>
/// <remarks>
/// All run reads are routed through <c>IRunDetailQueryService</c> to ensure a single authoritative
/// data-loading path. Result and evidence writes execute inside <see cref="IArchLucidUnitOfWork"/> for atomicity;
/// Authority <c>dbo.Runs</c> lifecycle is updated by dedicated orchestrators, not this application service.
/// </remarks>
public sealed class ArchitectureApplicationService(
    IRunDetailQueryService runDetailQueryService,
    IAgentResultRepository resultRepository,
    IUnifiedGoldenManifestReader unifiedGoldenManifestReader,
    IArchitectureRequestRepository requestRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IEvidenceBuilder evidenceBuilder,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
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

        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        return detail is null ? null : new GetRunResult(detail.Run, detail.Tasks, detail.Results);
    }

    public async Task<SubmitResultResult> SubmitAgentResultAsync(string runId, AgentResult? result, CancellationToken cancellationToken = default)
    {
        if (result is null)
            return new SubmitResultResult(false, null, "Agent result is required.", ApplicationServiceFailureKind.BadRequest);

        if (string.IsNullOrWhiteSpace(runId))
            return new SubmitResultResult(false, null, "RunId is required.", ApplicationServiceFailureKind.BadRequest);

        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
            return new SubmitResultResult(false, null, $"Run '{runId}' was not found.", ApplicationServiceFailureKind.RunNotFound);


        ArchitectureRun run = detail.Run;
        List<AgentTask> tasks = detail.Tasks;
        List<AgentResult> existingResults = detail.Results;

        if (!ResultSubmissionAllowedStatuses.Contains(run.Status))
        {
            string allowed = string.Join(" or ", ResultSubmissionAllowedStatuses.OrderBy(s => s.ToString()));
            return new SubmitResultResult(false, null,
                $"Run is in status '{run.Status}' and does not accept agent results. Only {allowed} runs can receive results.",
                ApplicationServiceFailureKind.BadRequest);
        }

        if (!string.Equals(result.RunId, runId, StringComparison.OrdinalIgnoreCase))

            return new SubmitResultResult(false, null,
                $"Result RunId '{result.RunId}' does not match route runId '{runId}'.",
                ApplicationServiceFailureKind.BadRequest);


        AgentTask? task = tasks.FirstOrDefault(t => string.Equals(t.TaskId, result.TaskId, StringComparison.Ordinal));
        if (task is null)

            return new SubmitResultResult(false, null,
                $"Task '{result.TaskId}' was not found for run '{runId}'.",
                ApplicationServiceFailureKind.ResourceNotFound);


        if (task.AgentType != result.AgentType)

            return new SubmitResultResult(false, null,
                $"Result AgentType '{result.AgentType}' does not match task AgentType '{task.AgentType}' for task '{result.TaskId}'.",
                ApplicationServiceFailureKind.BadRequest);


        if (existingResults.Any(r => string.Equals(r.TaskId, result.TaskId, StringComparison.Ordinal)))

            return new SubmitResultResult(false, null,
                $"A result for task '{result.TaskId}' has already been submitted for this run.",
                ApplicationServiceFailureKind.BadRequest);


        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            ArchitectureRunStatus newStatus = await SubmitAgentResultPersistAsync(
                runId,
                result,
                uow,
                cancellationToken);

            await uow.CommitAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformationAgentResultSubmitted(runId, result.ResultId, result.AgentType, newStatus);


            return new SubmitResultResult(true, result.ResultId, null);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>True when there is exactly one result for each required agent type and no extra types.</summary>
    private static bool HasAllRequiredAgentTypes(IReadOnlyList<AgentResult>? results)
    {
        if (results is null)
            return false;

        if (results.Count != RequiredAgentTypes.Count)
            return false;

        foreach (AgentType required in RequiredAgentTypes)

            if (results.Count(r => r.AgentType == required) != 1)
                return false;


        return true;
    }

    public async Task<GoldenManifest?> GetManifestAsync(string version, CancellationToken cancellationToken = default)
    {
        return await unifiedGoldenManifestReader.GetByVersionAsync(version, cancellationToken);
    }

    public async Task<SeedFakeResultsResult> SeedFakeResultsAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return new SeedFakeResultsResult(false, 0, "RunId is required.", ApplicationServiceFailureKind.BadRequest);

        ArchitectureRunDetail? detail = await runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
            return new SeedFakeResultsResult(false, 0, $"Run '{runId}' was not found.", ApplicationServiceFailureKind.RunNotFound);


        ArchitectureRun run = detail.Run;

        if (!ResultSubmissionAllowedStatuses.Contains(run.Status))
        {
            string allowed = string.Join(" or ", ResultSubmissionAllowedStatuses.OrderBy(s => s.ToString()));
            return new SeedFakeResultsResult(false, 0,
                $"Run is in status '{run.Status}' and does not accept results. Only {allowed} runs can be seeded.",
                ApplicationServiceFailureKind.BadRequest);
        }

        ArchitectureRequest? architectureRequest = await requestRepository.GetByIdAsync(run.RequestId, cancellationToken);
        if (architectureRequest is null)

            return new SeedFakeResultsResult(false, 0,
                $"ArchitectureRequest '{run.RequestId}' for run '{runId}' was not found.",
                ApplicationServiceFailureKind.ResourceNotFound);


        List<AgentTask> tasks = detail.Tasks;

        if (tasks.Count == 0)
            return new SeedFakeResultsResult(false, 0, "No tasks exist for this run.", ApplicationServiceFailureKind.BadRequest);


        List<AgentResult> existingResults = detail.Results;

        if (existingResults.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))

                logger.LogInformation(
                    "Fake results skipped (run already has results): RunId={RunId}, ExistingCount={Count}",
                    LogSanitizer.Sanitize(runId),
                    existingResults.Count);

            return new SeedFakeResultsResult(true, 0, null);
        }

        IReadOnlyList<AgentResult> fakeResults = FakeAgentResultFactory.CreateStarterResults(runId, tasks, architectureRequest);

        ArchitectureRunStatus newStatus = HasAllRequiredAgentTypes(fakeResults)
            ? ArchitectureRunStatus.ReadyForCommit
            : ArchitectureRunStatus.WaitingForResults;

        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            await SeedFakeResultsPersistAsync(runId, fakeResults, architectureRequest, uow, cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }

        if (logger.IsEnabled(LogLevel.Information))

            logger.LogInformation(
                "Fake results seeded: RunId={RunId}, ResultCount={ResultCount}, NewStatus={NewStatus}",
                LogSanitizer.Sanitize(runId),
                fakeResults.Count,
                newStatus);


        return new SeedFakeResultsResult(true, fakeResults.Count, null);
    }

    private async Task<ArchitectureRunStatus> SubmitAgentResultPersistAsync(
        string runId,
        AgentResult result,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        if (uow.SupportsExternalTransaction)
        {
            await resultRepository.CreateAsync(result, cancellationToken, uow.Connection, uow.Transaction);

            // Re-fetch results after insert so concurrent submissions see the full set and only one transition sets ReadyForCommit.
            IReadOnlyList<AgentResult> allResults = await resultRepository.GetByRunIdAsync(
                runId,
                cancellationToken,
                uow.Connection,
                uow.Transaction);
            bool hasAllRequiredAgentTypes = HasAllRequiredAgentTypes(allResults);
            ArchitectureRunStatus newStatus = hasAllRequiredAgentTypes
                ? ArchitectureRunStatus.ReadyForCommit
                : ArchitectureRunStatus.WaitingForResults;

            return newStatus;
        }

        await resultRepository.CreateAsync(result, cancellationToken);

        IReadOnlyList<AgentResult> allResultsMemory = await resultRepository.GetByRunIdAsync(runId, cancellationToken);
        bool hasAllRequired = HasAllRequiredAgentTypes(allResultsMemory);
        ArchitectureRunStatus newStatusMemory = hasAllRequired
            ? ArchitectureRunStatus.ReadyForCommit
            : ArchitectureRunStatus.WaitingForResults;

        return newStatusMemory;
    }

    private async Task SeedFakeResultsPersistAsync(
        string runId,
        IReadOnlyList<AgentResult> fakeResults,
        ArchitectureRequest request,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        // CommitRunAsync requires a persisted evidence package (normally written during ExecuteRun). Dev-only seed
        // skips execute, so create the package here when missing.
        AgentEvidencePackage? existingPackage = await agentEvidencePackageRepository.GetByRunIdAsync(runId, cancellationToken);

        if (existingPackage is null)
        {
            AgentEvidencePackage package = await evidenceBuilder.BuildAsync(runId, request, cancellationToken);

            if (uow.SupportsExternalTransaction)
                await agentEvidencePackageRepository.CreateAsync(package, cancellationToken, uow.Connection, uow.Transaction);
            else
                await agentEvidencePackageRepository.CreateAsync(package, cancellationToken);
        }

        if (uow.SupportsExternalTransaction)

            await resultRepository.CreateManyAsync(fakeResults, cancellationToken, uow.Connection, uow.Transaction);

        else

            await resultRepository.CreateManyAsync(fakeResults, cancellationToken);

    }
}

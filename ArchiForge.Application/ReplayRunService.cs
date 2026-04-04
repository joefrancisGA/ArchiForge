using System.Transactions;

using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Agents;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

namespace ArchiForge.Application;

/// <summary>
/// Replays an existing architecture run by cloning its tasks and evidence, re-executing agents,
/// and optionally committing the result as a new manifest version.
/// Used by <see cref="ArchiForge.Application.Determinism.DeterminismCheckService"/> for multi-iteration
/// determinism checks and by comparison services for regenerating stored payloads.
/// </summary>
public sealed class ReplayRunService(
    IAgentExecutorResolver agentExecutorResolver,
    IDecisionEngineService decisionEngineService,
    IArchitectureRequestRepository requestRepository,
    IRunDetailQueryService runDetailQueryService,
    IArchitectureRunRepository runRepository,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository)
    : IReplayRunService
{
    /// <summary>
    /// Creates a new run record seeded from <paramref name="originalRunId"/>, re-executes agents,
    /// and (when <paramref name="commitReplay"/> is <c>true</c>) commits a new manifest.
    /// </summary>
    /// <exception cref="RunNotFoundException">Thrown when <paramref name="originalRunId"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the original run has no tasks, no evidence package, or merge fails.
    /// </exception>
    public async Task<ReplayRunResult> ReplayAsync(
        string originalRunId,
        string executionMode = ExecutionModes.Current,
        bool commitReplay = false,
        string? manifestVersionOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionMode);

        ArchitectureRunDetail sourceDetail = await runDetailQueryService.GetRunDetailAsync(originalRunId, cancellationToken)
                                             ?? throw new RunNotFoundException(originalRunId);

        ArchitectureRun originalRun = sourceDetail.Run;
        List<AgentTask> tasks = sourceDetail.Tasks;

        cancellationToken.ThrowIfCancellationRequested();

        if (tasks.Count == 0)
        
            throw new InvalidOperationException($"No tasks found for run '{originalRunId}'.");
        

        ArchitectureRequest request = await requestRepository.GetByIdAsync(originalRun.RequestId, cancellationToken)
                                      ?? throw new InvalidOperationException($"Request '{originalRun.RequestId}' not found.");

        AgentEvidencePackage evidence = await agentEvidencePackageRepository.GetByRunIdAsync(originalRunId, cancellationToken)
                                        ?? throw new InvalidOperationException($"Evidence package for run '{originalRunId}' not found.");

        string replayRunId = Guid.NewGuid().ToString("N");

        ArchitectureRun replayRun = new()
        {
            RunId = replayRunId,
            RequestId = originalRun.RequestId,
            Status = ArchitectureRunStatus.Created,
            CreatedUtc = DateTime.UtcNow
        };

        await runRepository.CreateAsync(replayRun, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        List<AgentTask> replayTasks = tasks
            .Select(t => new AgentTask
            {
                TaskId = Guid.NewGuid().ToString("N"),
                RunId = replayRunId,
                AgentType = t.AgentType,
                Objective = t.Objective,
                Status = AgentTaskStatus.Created,
                CreatedUtc = DateTime.UtcNow,
                CompletedUtc = null,
                EvidenceBundleRef = t.EvidenceBundleRef,
                AllowedTools = t.AllowedTools.ToList(),
                AllowedSources = t.AllowedSources.ToList()
            })
            .ToList();

        AgentEvidencePackage replayEvidence = CloneEvidenceForReplay(evidence, replayRunId);

        IAgentExecutor executor = agentExecutorResolver.Resolve(executionMode);
        IReadOnlyList<AgentResult> results = await executor.ExecuteAsync(
            replayRunId,
            request,
            replayEvidence,
            replayTasks,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        GoldenManifest? manifest = null;
        List<DecisionTrace> decisionTraces = [];
        List<string> warnings = [];

        if (!commitReplay)
            return new ReplayRunResult
            {
                OriginalRunId = originalRunId,
                ReplayRunId = replayRunId,
                ExecutionMode = executionMode,
                Results = results.ToList(),
                Manifest = manifest,
                DecisionTraces = decisionTraces,
                Warnings = warnings
            };
        
        string manifestVersion = string.IsNullOrWhiteSpace(manifestVersionOverride)
            ? BuildReplayManifestVersion(originalRun.CurrentManifestVersion)
            : manifestVersionOverride;

        DecisionMergeResult merge = decisionEngineService.MergeResults(
            replayRunId,
            request,
            manifestVersion,
            results,
            evaluations: [],
            decisionNodes: [],
            parentManifestVersion: originalRun.CurrentManifestVersion);

        if (!merge.Success)
        
            throw new InvalidOperationException(
                $"Replay merge failed: {string.Join("; ", merge.Errors)}");
        

        manifest = merge.Manifest;
        decisionTraces = merge.DecisionTraces;
        warnings = merge.Warnings;

        using TransactionScope scope = new(
            TransactionScopeOption.Required,
            TransactionScopeAsyncFlowOption.Enabled);

        await manifestRepository.CreateAsync(manifest, cancellationToken);
        await decisionTraceRepository.CreateManyAsync(decisionTraces, cancellationToken);
        await runRepository.UpdateStatusAsync(
            replayRunId,
            ArchitectureRunStatus.Committed,
            currentManifestVersion: manifest.Metadata.ManifestVersion,
            completedUtc: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        scope.Complete();

        return new ReplayRunResult
        {
            OriginalRunId = originalRunId,
            ReplayRunId = replayRunId,
            ExecutionMode = executionMode,
            Results = results.ToList(),
            Manifest = manifest,
            DecisionTraces = decisionTraces,
            Warnings = warnings
        };
    }

    //private async Task PersistReplayCommitRowsAsync(
    //    string replayRunId,
    //    GoldenManifest manifest,
    //    List<DecisionTrace> decisionTraces,
    //    CancellationToken cancellationToken)
    //{
    //    await manifestRepository.CreateAsync(manifest, cancellationToken);
    //    await decisionTraceRepository.CreateManyAsync(decisionTraces, cancellationToken);
    //    await runRepository.UpdateStatusAsync(
    //        replayRunId,
    //        ArchitectureRunStatus.Committed,
    //        currentManifestVersion: manifest.Metadata.ManifestVersion,
    //        completedUtc: DateTime.UtcNow,
    //        cancellationToken: cancellationToken);
    //}

    /// <summary>
    /// Creates a deep copy of <paramref name="original"/> bound to <paramref name="replayRunId"/>.
    /// A clone is required so the replay run's evidence is isolated from the original run's mutable
    /// collections — shared references would corrupt both runs if either were mutated.
    /// </summary>
    private static AgentEvidencePackage CloneEvidenceForReplay(
        AgentEvidencePackage original,
        string replayRunId)
    {
        return new AgentEvidencePackage
        {
            EvidencePackageId = Guid.NewGuid().ToString("N"),
            RunId = replayRunId,
            RequestId = original.RequestId,
            SystemName = original.SystemName,
            Environment = original.Environment,
            CloudProvider = original.CloudProvider,
            Request = new RequestEvidence
            {
                Description = original.Request.Description,
                Constraints = original.Request.Constraints.ToList(),
                RequiredCapabilities = original.Request.RequiredCapabilities.ToList(),
                Assumptions = original.Request.Assumptions.ToList()
            },
            Policies = original.Policies.Select(p => new PolicyEvidence
            {
                PolicyId = p.PolicyId,
                Title = p.Title,
                Summary = p.Summary,
                RequiredControls = p.RequiredControls.ToList(),
                Tags = p.Tags.ToList()
            }).ToList(),
            ServiceCatalog = original.ServiceCatalog.Select(s => new ServiceCatalogEvidence
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                Category = s.Category,
                Summary = s.Summary,
                Tags = s.Tags.ToList(),
                RecommendedUseCases = s.RecommendedUseCases.ToList()
            }).ToList(),
            Patterns = original.Patterns.Select(p => new PatternEvidence
            {
                PatternId = p.PatternId,
                Name = p.Name,
                Summary = p.Summary,
                ApplicableCapabilities = p.ApplicableCapabilities.ToList(),
                SuggestedServices = p.SuggestedServices.ToList()
            }).ToList(),
            PriorManifest = original.PriorManifest is null
                ? null
                : new PriorManifestEvidence
                {
                    ManifestVersion = original.PriorManifest.ManifestVersion,
                    Summary = original.PriorManifest.Summary,
                    ExistingServices = original.PriorManifest.ExistingServices.ToList(),
                    ExistingDatastores = original.PriorManifest.ExistingDatastores.ToList(),
                    ExistingRequiredControls = original.PriorManifest.ExistingRequiredControls.ToList()
                },
            Notes = original.Notes.Select(n => new EvidenceNote
            {
                NoteType = n.NoteType,
                Message = n.Message
            }).ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Derives a replay manifest version by appending <c>-replay</c> to the current version,
    /// or returns <c>v1-replay</c> when no current version exists.
    /// </summary>
    private static string BuildReplayManifestVersion(string? currentManifestVersion)
    {
        return string.IsNullOrWhiteSpace(currentManifestVersion) ? "v1-replay" : $"{currentManifestVersion}-replay";
    }
}

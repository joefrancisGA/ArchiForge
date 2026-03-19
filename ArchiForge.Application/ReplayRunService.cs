using ArchiForge.Application.Agents;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

namespace ArchiForge.Application;

public sealed class ReplayRunService(
    IAgentExecutorResolver agentExecutorResolver,
    IDecisionEngineService decisionEngineService,
    IArchitectureRequestRepository requestRepository,
    IArchitectureRunRepository runRepository,
    IAgentTaskRepository taskRepository,
    IAgentResultRepository resultRepository,
    IGoldenManifestRepository manifestRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IAgentEvidencePackageRepository agentEvidencePackageRepository)
    : IReplayRunService
{
    private readonly IAgentResultRepository _resultRepository = resultRepository;

    public async Task<ReplayRunResult> ReplayAsync(
        string originalRunId,
        string executionMode = "Current",
        bool commitReplay = false,
        string? manifestVersionOverride = null,
        CancellationToken cancellationToken = default)
    {
        var originalRun = await runRepository.GetByIdAsync(originalRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Original run '{originalRunId}' not found.");

        var request = await requestRepository.GetByIdAsync(originalRun.RequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{originalRun.RequestId}' not found.");

        var tasks = await taskRepository.GetByRunIdAsync(originalRunId, cancellationToken);
        if (tasks.Count == 0)
        {
            throw new InvalidOperationException($"No tasks found for run '{originalRunId}'.");
        }

        var evidence = await agentEvidencePackageRepository.GetByRunIdAsync(originalRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Evidence package for run '{originalRunId}' not found.");

        var replayRunId = Guid.NewGuid().ToString("N");

        var replayTasks = tasks
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

        var replayEvidence = CloneEvidenceForReplay(evidence, replayRunId);

        var executor = agentExecutorResolver.Resolve(executionMode);
        var results = await executor.ExecuteAsync(
            replayRunId,
            request,
            replayEvidence,
            replayTasks,
            cancellationToken);

        GoldenManifest? manifest = null;
        List<DecisionTrace> decisionTraces = [];
        List<string> warnings = [];

        if (commitReplay)
        {
            var manifestVersion = string.IsNullOrWhiteSpace(manifestVersionOverride)
                ? BuildReplayManifestVersion(originalRun.CurrentManifestVersion)
                : manifestVersionOverride;

            var merge = decisionEngineService.MergeResults(
                replayRunId,
                request,
                manifestVersion,
                results,
                evaluations: [],
                decisionNodes: [],
                parentManifestVersion: originalRun.CurrentManifestVersion);

            if (!merge.Success)
            {
                throw new InvalidOperationException(
                    $"Replay merge failed: {string.Join("; ", merge.Errors)}");
            }

            manifest = merge.Manifest;
            decisionTraces = merge.DecisionTraces;
            warnings = merge.Warnings;

            await manifestRepository.CreateAsync(manifest, cancellationToken);
            await decisionTraceRepository.CreateManyAsync(decisionTraces, cancellationToken);
        }

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

    private static string BuildReplayManifestVersion(string? currentManifestVersion)
    {
        if (string.IsNullOrWhiteSpace(currentManifestVersion))
        {
            return "v1-replay";
        }

        return $"{currentManifestVersion}-replay";
    }
}

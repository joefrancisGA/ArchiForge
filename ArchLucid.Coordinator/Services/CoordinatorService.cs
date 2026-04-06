using ArchiForge.ContextIngestion.Mapping;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Orchestration;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Coordinator.Services;

/// <summary>
/// Validates <see cref="ArchitectureRequest"/> input, delegates persistence to <see cref="IAuthorityRunOrchestrator"/>, and assembles <see cref="CoordinationResult"/> (run, evidence bundle, starter tasks, graph shell).
/// </summary>
public sealed class CoordinatorService(
    IAuthorityRunOrchestrator authorityRunOrchestrator,
    ILogger<CoordinatorService> logger) : ICoordinatorService
{
    /// <inheritdoc />
    public async Task<CoordinationResult> CreateRunAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CoordinationResult output = new();

        List<string> validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            output.Errors.AddRange(validationErrors);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Coordination rejected (validation): RequestId={RequestId}, SystemName={SystemName}, Errors={Errors}",
                    request.RequestId,
                    request.SystemName,
                    string.Join("; ", validationErrors));
            }

            return output;
        }

        EvidenceBundle evidenceBundle = RunStarterTaskFactory.BuildEvidenceBundle(request);

        RunRecord authorityRun = await authorityRunOrchestrator.ExecuteAsync(
            ContextIngestionRequestMapper.FromArchitectureRequest(request),
            cancellationToken,
            evidenceBundle.EvidenceBundleId);

        bool deferred = authorityRun.ContextSnapshotId is null;

        string runId = authorityRun.RunId.ToString("N");
        ArchitectureRun run = BuildRunFromAuthority(authorityRun, request, deferred);

        List<AgentTask> tasks = deferred
            ? []
            : RunStarterTaskFactory.BuildStarterTasks(runId, evidenceBundle, request);

        run.TaskIds = [.. tasks.Select(t => t.TaskId)];

        output.Run = run;
        output.EvidenceBundle = evidenceBundle;
        output.Tasks = tasks;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Coordination completed: RunId={RunId}, RequestId={RequestId}, StarterTaskCount={TaskCount}, EvidenceBundleId={EvidenceBundleId}, Deferred={Deferred}",
                run.RunId,
                request.RequestId,
                tasks.Count,
                evidenceBundle.EvidenceBundleId,
                deferred);
        }

        return output;
    }

    private static List<string> ValidateRequest(ArchitectureRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.RequestId))
            errors.Add("RequestId is required.");

        if (string.IsNullOrWhiteSpace(request.SystemName))
            errors.Add("SystemName is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            errors.Add("Description is required.");

        return errors;
    }

    private static ArchitectureRun BuildRunFromAuthority(RunRecord authorityRun, ArchitectureRequest request, bool deferred)
    {
        return new ArchitectureRun
        {
            RunId = authorityRun.RunId.ToString("N"),
            RequestId = request.RequestId,
            Status = deferred ? ArchitectureRunStatus.Created : ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = null,
            CurrentManifestVersion = null,
            ContextSnapshotId = authorityRun.ContextSnapshotId?.ToString("N"),
            GraphSnapshotId = authorityRun.GraphSnapshotId,
            FindingsSnapshotId = authorityRun.FindingsSnapshotId,
            GoldenManifestId = authorityRun.GoldenManifestId,
            DecisionTraceId = authorityRun.DecisionTraceId,
            ArtifactBundleId = authorityRun.ArtifactBundleId,
            TaskIds = []
        };
    }
}

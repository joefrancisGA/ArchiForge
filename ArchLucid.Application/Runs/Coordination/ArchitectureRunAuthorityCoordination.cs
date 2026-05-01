using ArchLucid.ContextIngestion.Mapping;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Runs.Coordination;

/// <summary>
///     Validates <see cref="ArchitectureRequest" /> input, delegates persistence to
///     <see cref="IAuthorityRunOrchestrator" />, and assembles <see cref="CoordinationResult" /> (run, evidence bundle,
///     starter tasks).
/// </summary>
public sealed class ArchitectureRunAuthorityCoordination(
    IAuthorityRunOrchestrator authorityRunOrchestrator,
    IRunRepository runRepository,
    IScopeContextProvider scopeContextProvider,
    ILogger<ArchitectureRunAuthorityCoordination> logger) : IArchitectureRunAuthorityCoordination
{
    private readonly IAuthorityRunOrchestrator _authorityRunOrchestrator =
        authorityRunOrchestrator ?? throw new ArgumentNullException(nameof(authorityRunOrchestrator));

    private readonly ILogger<ArchitectureRunAuthorityCoordination> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

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

            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarningWithThreeSanitizedUserStrings(
                    "Coordination rejected (validation): RequestId={RequestId}, SystemName={SystemName}, Errors={Errors}",
                    request.RequestId,
                    request.SystemName,
                    string.Join("; ", validationErrors));

            return output;
        }

        EvidenceBundle evidenceBundle = RunStarterTaskFactory.BuildEvidenceBundle(request);

        RunRecord authorityRun = await _authorityRunOrchestrator.ExecuteAsync(
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

        await PatchAuthorityRunHeaderAsync(
            authorityRun.RunId,
            request.RequestId,
            deferred,
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))

            _logger.LogInformation(
                "Coordination completed: RunId={RunId}, RequestId={RequestId}, StarterTaskCount={TaskCount}, EvidenceBundleId={EvidenceBundleId}, Deferred={Deferred}",
                run.RunId,
                LogSanitizer.Sanitize(request.RequestId),
                tasks.Count,
                evidenceBundle.EvidenceBundleId,
                deferred);

        return output;
    }

    private async Task PatchAuthorityRunHeaderAsync(
        Guid authorityRunId,
        string requestId,
        bool deferred,
        CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunRecord? header = await _runRepository.GetByIdAsync(scope, authorityRunId, cancellationToken);

        if (header is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    "Authority run header {RunId} not found for lifecycle patch (RequestId={RequestId}).",
                    authorityRunId,
                    LogSanitizer.Sanitize(requestId));

            return;
        }

        header.ArchitectureRequestId = requestId;
        header.LegacyRunStatus = deferred
            ? nameof(ArchitectureRunStatus.Created)
            : nameof(ArchitectureRunStatus.TasksGenerated);

        await _runRepository.UpdateAsync(header, cancellationToken);
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

    private static ArchitectureRun BuildRunFromAuthority(RunRecord authorityRun, ArchitectureRequest request,
        bool deferred)
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

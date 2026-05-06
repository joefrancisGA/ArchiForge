using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Builds a full <see cref = "EndToEndReplayComparisonReport"/> by loading both runs through
///     <see cref = "IRunDetailQueryService"/>, diffing agent results, manifests, and export records
///     side-by-side, and appending human-readable interpretation notes.
/// </summary>
/// <remarks>
///     Warnings are added to <see cref = "EndToEndReplayComparisonReport.Warnings"/> rather than
///     thrown when optional data (manifests, exports) is missing for one or both runs.
///     Throws <see cref = "RunNotFoundException"/> when either run cannot be resolved.
/// </remarks>
public sealed class EndToEndReplayComparisonService(IRunDetailQueryService runDetailQueryService, IRunExportRecordRepository runExportRecordRepository, IAgentResultDiffService agentResultDiffService, IManifestDiffService manifestDiffService, IExportRecordDiffService exportRecordDiffService) : IEndToEndReplayComparisonService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runDetailQueryService, runExportRecordRepository, agentResultDiffService, manifestDiffService, exportRecordDiffService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.IRunDetailQueryService runDetailQueryService, ArchLucid.Persistence.Data.Repositories.IRunExportRecordRepository runExportRecordRepository, ArchLucid.Application.Diffs.IAgentResultDiffService agentResultDiffService, ArchLucid.Application.Diffs.IManifestDiffService manifestDiffService, ArchLucid.Application.Analysis.IExportRecordDiffService exportRecordDiffService)
    {
        ArgumentNullException.ThrowIfNull(runDetailQueryService);
        ArgumentNullException.ThrowIfNull(runExportRecordRepository);
        ArgumentNullException.ThrowIfNull(agentResultDiffService);
        ArgumentNullException.ThrowIfNull(manifestDiffService);
        ArgumentNullException.ThrowIfNull(exportRecordDiffService);
        return (byte)0;
    }

    public async Task<EndToEndReplayComparisonReport> BuildAsync(string leftRunId, string rightRunId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leftRunId);
        ArgumentNullException.ThrowIfNull(rightRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(leftRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rightRunId);
        ArchitectureRunDetail leftDetail = await runDetailQueryService.GetRunDetailAsync(leftRunId, cancellationToken) ?? throw new RunNotFoundException(leftRunId);
        ArchitectureRunDetail rightDetail = await runDetailQueryService.GetRunDetailAsync(rightRunId, cancellationToken) ?? throw new RunNotFoundException(rightRunId);
        ArchitectureRun leftRun = leftDetail.Run;
        ArchitectureRun rightRun = rightDetail.Run;
        EndToEndReplayComparisonReport report = new()
        {
            LeftRunId = leftRunId,
            RightRunId = rightRunId,
            RunDiff = BuildRunDiff(leftRun, rightRun)
        };
        List<AgentResult> leftResults = leftDetail.Results;
        List<AgentResult> rightResults = rightDetail.Results;
        if (leftResults.Count > 0 || rightResults.Count > 0)
            report.AgentResultDiff = agentResultDiffService.Compare(leftRunId, leftResults, rightRunId, rightResults);
        else
            report.Warnings.Add("Neither run contained agent results.");
        if (!string.IsNullOrWhiteSpace(leftRun.CurrentManifestVersion) && !string.IsNullOrWhiteSpace(rightRun.CurrentManifestVersion))
            if (leftDetail.Manifest is not null && rightDetail.Manifest is not null)
                report.ManifestDiff = manifestDiffService.Compare(leftDetail.Manifest, rightDetail.Manifest);
            else
                report.Warnings.Add("One or both manifests were unavailable for manifest comparison.");
        IReadOnlyList<RunExportRecord> leftExports = await runExportRecordRepository.GetByRunIdAsync(leftRunId, cancellationToken);
        IReadOnlyList<RunExportRecord> rightExports = await runExportRecordRepository.GetByRunIdAsync(rightRunId, cancellationToken);
        // Match by ExportType so that ordering differences between runs don't produce nonsensical diffs.
        Dictionary<string, RunExportRecord> leftByType = leftExports.GroupBy(e => e.ExportType, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, RunExportRecord> rightByType = rightExports.GroupBy(e => e.ExportType, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (string exportType in leftByType.Keys.Union(rightByType.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        {
            bool hasLeft = leftByType.TryGetValue(exportType, out RunExportRecord? leftRecord);
            bool hasRight = rightByType.TryGetValue(exportType, out RunExportRecord? rightRecord);
            if (hasLeft && hasRight)
                report.ExportDiffs.Add(exportRecordDiffService.Compare(leftRecord!, rightRecord!));
            else if (!hasLeft)
                report.Warnings.Add($"Export type '{exportType}' exists on the right run but not the left.");
            else
                report.Warnings.Add($"Export type '{exportType}' exists on the left run but not the right.");
        }

        AddInterpretationNotes(report);
        return report;
    }

    private static RunMetadataDiffResult BuildRunDiff(ArchitectureRun leftRun, ArchitectureRun rightRun)
    {
        RunMetadataDiffResult result = new();
        AddIfChanged(result.ChangedFields, "RequestId", leftRun.RequestId, rightRun.RequestId);
        AddIfChanged(result.ChangedFields, "Status", leftRun.Status, rightRun.Status);
        AddIfChanged(result.ChangedFields, "CurrentManifestVersion", leftRun.CurrentManifestVersion, rightRun.CurrentManifestVersion);
        AddIfChanged(result.ChangedFields, "CompletedUtc", leftRun.CompletedUtc, rightRun.CompletedUtc);
        result.RequestIdsDiffer = !string.Equals(leftRun.RequestId, rightRun.RequestId, StringComparison.OrdinalIgnoreCase);
        result.ManifestVersionsDiffer = !string.Equals(leftRun.CurrentManifestVersion, rightRun.CurrentManifestVersion, StringComparison.OrdinalIgnoreCase);
        result.StatusDiffers = !Equals(leftRun.Status, rightRun.Status);
        result.CompletionStateDiffers = leftRun.CompletedUtc is null != rightRun.CompletedUtc is null;
        return result;
    }

    private static void AddInterpretationNotes(EndToEndReplayComparisonReport report)
    {
        if (report.AgentResultDiff is not null && report.ManifestDiff is not null)
        {
            bool agentChanged = report.AgentResultDiff.AgentDeltas.Any(d => d.AddedClaims.Count > 0 || d.RemovedClaims.Count > 0 || d.AddedFindings.Count > 0 || d.RemovedFindings.Count > 0 || d.AddedRequiredControls.Count > 0 || d.RemovedRequiredControls.Count > 0 || d.AddedWarnings.Count > 0 || d.RemovedWarnings.Count > 0);
            bool manifestChanged = report.ManifestDiff.AddedServices.Count > 0 || report.ManifestDiff.RemovedServices.Count > 0 || report.ManifestDiff.AddedDatastores.Count > 0 || report.ManifestDiff.RemovedDatastores.Count > 0 || report.ManifestDiff.AddedRequiredControls.Count > 0 || report.ManifestDiff.RemovedRequiredControls.Count > 0 || report.ManifestDiff.AddedRelationships.Count > 0 || report.ManifestDiff.RemovedRelationships.Count > 0;
            if (agentChanged && manifestChanged)
                report.InterpretationNotes.Add("Both agent outputs and resolved manifest changed, suggesting upstream proposal drift propagated into architecture state.");
            else if (!agentChanged && manifestChanged)
                report.InterpretationNotes.Add("The manifest changed without meaningful agent drift, which suggests merge logic or manifest ancestry differences.");
            else if (agentChanged && !manifestChanged)
                report.InterpretationNotes.Add("Agent outputs changed, but the resolved manifest remained stable, suggesting merge logic absorbed or normalized the drift.");
            else
                report.InterpretationNotes.Add("Neither agent outputs nor manifest changed materially.");
        }

        if (report.ExportDiffs.Any(d => d.ChangedTopLevelFields.Count > 0 || d.RequestDiff.ChangedFlags.Count > 0 || d.RequestDiff.ChangedValues.Count > 0))
            report.InterpretationNotes.Add("Export configuration differences were detected, so document outputs may differ even when architecture state is similar.");
    }

    private static void AddIfChanged<T>(List<string> target, string fieldName, T left, T right)
    {
        if (!EqualityComparer<T>.Default.Equals(left, right))
            target.Add(fieldName);
    }
}
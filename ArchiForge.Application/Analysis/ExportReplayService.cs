using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Replays a persisted <see cref="RunExportRecord"/> by rehydrating the original analysis request and
/// regenerating the export artifact (e.g. consulting DOCX), optionally recording the replay as a new export record.
/// </summary>
public sealed class ExportReplayService(
    IRunExportRecordRepository runExportRecordRepository,
    IArchitectureAnalysisService architectureAnalysisService,
    IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService,
    IRunExportAuditService runExportAuditService)
    : IExportReplayService
{
    /// <summary>
    /// Replays the export identified by <see cref="ReplayExportRequest.ExportRecordId"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the export record does not exist, its persisted request cannot be rehydrated,
    /// or the export type is not supported for replay.
    /// </exception>
    public async Task<ReplayExportResult> ReplayAsync(
        ReplayExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExportRecordId);

        RunExportRecord? record = await runExportRecordRepository.GetByIdAsync(
            request.ExportRecordId,
            cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException(
                $"Export record '{request.ExportRecordId}' was not found.");
        }

        PersistedAnalysisExportRequest persistedRequest = AnalysisExportRequestRehydrator.Rehydrate(record)
                                                          ?? throw new InvalidOperationException(
                                                              $"Export record '{request.ExportRecordId}' does not contain a persisted analysis request.");

        ArchitectureAnalysisRequest analysisRequest = new()
        {
            RunId = record.RunId,
            IncludeEvidence = persistedRequest.IncludeEvidence,
            IncludeExecutionTraces = persistedRequest.IncludeExecutionTraces,
            IncludeManifest = persistedRequest.IncludeManifest,
            IncludeDiagram = persistedRequest.IncludeDiagram,
            IncludeSummary = persistedRequest.IncludeSummary,
            IncludeDeterminismCheck = persistedRequest.IncludeDeterminismCheck,
            DeterminismIterations = persistedRequest.DeterminismIterations,
            IncludeManifestCompare = persistedRequest.IncludeManifestCompare,
            CompareManifestVersion = persistedRequest.CompareManifestVersion,
            IncludeAgentResultCompare = persistedRequest.IncludeAgentResultCompare,
            CompareRunId = persistedRequest.CompareRunId
        };

        ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(
            analysisRequest,
            cancellationToken);

        return record.ExportType switch
        {
            "analysis-report-consulting-docx" => await ReplayConsultingDocxAsync(
                record,
                persistedRequest,
                report,
                request.RecordReplayExport,
                cancellationToken),

            _ => throw new InvalidOperationException(
                $"Replay is not supported for export type '{record.ExportType}'.")
        };
    }

    private async Task<ReplayExportResult> ReplayConsultingDocxAsync(
        RunExportRecord record,
        PersistedAnalysisExportRequest persistedRequest,
        ArchitectureAnalysisReport report,
        bool recordReplayExport,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await consultingDocxExportService.GenerateDocxAsync(
            report,
            cancellationToken);

        string replayFileName = BuildReplayFileName(record.FileName);

        if (recordReplayExport)
        {
            await runExportAuditService.RecordAsync(
                runId: record.RunId,
                exportType: record.ExportType,
                format: record.Format,
                fileName: replayFileName,
                templateProfile: record.TemplateProfile,
                templateProfileDisplayName: record.TemplateProfileDisplayName,
                wasAutoSelected: record.WasAutoSelected,
                resolutionReason: record.ResolutionReason,
                manifestVersion: report.Manifest?.Metadata.ManifestVersion,
                analysisRequest: persistedRequest,
                notes: $"Replay generated from export record {record.ExportRecordId}.",
                cancellationToken: cancellationToken);
        }

        return new ReplayExportResult
        {
            ExportRecordId = record.ExportRecordId,
            RunId = record.RunId,
            ExportType = record.ExportType,
            Format = record.Format,
            FileName = replayFileName,
            Content = bytes,
            TemplateProfile = record.TemplateProfile,
            TemplateProfileDisplayName = record.TemplateProfileDisplayName,
            WasAutoSelected = record.WasAutoSelected,
            ResolutionReason = record.ResolutionReason
        };
    }

    /// <summary>
    /// Appends <c>_replay</c> to the base file name while preserving the original extension.
    /// Returns <c>replayed_export.docx</c> when the original name is blank.
    /// </summary>
    private static string BuildReplayFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return "replayed_export.docx";
        }

        string extension = Path.GetExtension(originalFileName);
        string baseName = Path.GetFileNameWithoutExtension(originalFileName);

        return $"{baseName}_replay{extension}";
    }
}


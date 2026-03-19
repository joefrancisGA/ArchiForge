using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class ExportReplayService(
    IRunExportRecordRepository runExportRecordRepository,
    IArchitectureAnalysisService architectureAnalysisService,
    IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService,
    IRunExportAuditService runExportAuditService)
    : IExportReplayService
{
    public async Task<ReplayExportResult> ReplayAsync(
        ReplayExportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExportRecordId))
        {
            throw new InvalidOperationException("ExportRecordId is required.");
        }

        var record = await runExportRecordRepository.GetByIdAsync(
            request.ExportRecordId,
            cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException(
                $"Export record '{request.ExportRecordId}' was not found.");
        }

        var persistedRequest = AnalysisExportRequestRehydrator.Rehydrate(record)
            ?? throw new InvalidOperationException(
                $"Export record '{request.ExportRecordId}' does not contain a persisted analysis request.");

        var analysisRequest = new ArchitectureAnalysisRequest
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

        var report = await architectureAnalysisService.BuildAsync(
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
        var bytes = await consultingDocxExportService.GenerateDocxAsync(
            report,
            cancellationToken);

        var replayFileName = BuildReplayFileName(record.FileName);

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

    private static string BuildReplayFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return "replayed_export.docx";
        }

        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);

        return $"{baseName}_replay{extension}";
    }
}


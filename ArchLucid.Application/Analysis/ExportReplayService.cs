using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Replays a persisted <see cref = "RunExportRecord"/> by rehydrating the original analysis request and
///     regenerating the export artifact (consulting or standard analysis DOCX), optionally recording the replay as a new
///     export record.
/// </summary>
public sealed class ExportReplayService(IRunExportRecordRepository runExportRecordRepository, IArchitectureAnalysisService architectureAnalysisService, IArchitectureAnalysisDocxExportService analysisDocxExportService, IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService, IRunExportAuditService runExportAuditService) : IExportReplayService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runExportRecordRepository, architectureAnalysisService, analysisDocxExportService, consultingDocxExportService, runExportAuditService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Data.Repositories.IRunExportRecordRepository runExportRecordRepository, ArchLucid.Application.Analysis.IArchitectureAnalysisService architectureAnalysisService, ArchLucid.Application.Analysis.IArchitectureAnalysisDocxExportService analysisDocxExportService, ArchLucid.Application.Analysis.IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService, ArchLucid.Application.Analysis.IRunExportAuditService runExportAuditService)
    {
        ArgumentNullException.ThrowIfNull(runExportRecordRepository);
        ArgumentNullException.ThrowIfNull(architectureAnalysisService);
        ArgumentNullException.ThrowIfNull(analysisDocxExportService);
        ArgumentNullException.ThrowIfNull(consultingDocxExportService);
        ArgumentNullException.ThrowIfNull(runExportAuditService);
        return (byte)0;
    }

    private const string ExportTypeConsultingDocx = "analysis-report-consulting-docx";
    /// <summary>
    ///     Standard (non-consulting) analysis DOCX exports; must match the <see cref = "RunExportRecord.ExportType"/>
    ///     stored when those exports are audited.
    /// </summary>
    private const string ExportTypeAnalysisDocx = "analysis-report-docx";
    private const string FallbackReplayFileName = "replayed_export.docx";
    /// <summary>
    ///     Replays the export identified by <see cref = "ReplayExportRequest.ExportRecordId"/>.
    /// </summary>
    /// <exception cref = "InvalidOperationException">
    ///     Thrown when the export record does not exist, its persisted request cannot be rehydrated,
    ///     or the export type is not supported for replay.
    /// </exception>
    public async Task<ReplayExportResult> ReplayAsync(ReplayExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExportRecordId);
        RunExportRecord? record = await runExportRecordRepository.GetByIdAsync(request.ExportRecordId, cancellationToken);
        if (record is null)
            throw new InvalidOperationException($"Export record '{request.ExportRecordId}' was not found.");
        PersistedAnalysisExportRequest persistedRequest = AnalysisExportRequestRehydrator.Rehydrate(record) ?? throw new InvalidOperationException($"Export record '{request.ExportRecordId}' does not contain a persisted analysis request.");
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
        ArchitectureAnalysisReport report = await architectureAnalysisService.BuildAsync(analysisRequest, cancellationToken);
        return record.ExportType switch
        {
            ExportTypeConsultingDocx => await ReplayDocxAsync(record, persistedRequest, report, request.RecordReplayExport, consultingDocxExportService.GenerateDocxAsync, cancellationToken),
            ExportTypeAnalysisDocx => await ReplayDocxAsync(record, persistedRequest, report, request.RecordReplayExport, analysisDocxExportService.GenerateDocxAsync, cancellationToken),
            _ => throw new InvalidOperationException($"Replay is not supported for export type '{record.ExportType}'.")};
    }

    private async Task<ReplayExportResult> ReplayDocxAsync(RunExportRecord record, PersistedAnalysisExportRequest persistedRequest, ArchitectureAnalysisReport report, bool recordReplayExport, Func<ArchitectureAnalysisReport, CancellationToken, Task<byte[]>> generateDocxAsync, CancellationToken cancellationToken)
    {
        byte[] bytes = await generateDocxAsync(report, cancellationToken);
        string replayFileName = BuildReplayFileName(record.FileName);
        string? recordedReplayExportRecordId = null;
        if (!recordReplayExport)
            return new ReplayExportResult
            {
                ExportRecordId = record.ExportRecordId,
                RecordedReplayExportRecordId = recordedReplayExportRecordId,
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
        RunExportRecord persisted = await runExportAuditService.RecordAsync(record.RunId, record.ExportType, record.Format, replayFileName, record.TemplateProfile, record.TemplateProfileDisplayName, record.WasAutoSelected, record.ResolutionReason, report.Manifest?.Metadata.ManifestVersion, persistedRequest, $"Replay generated from export record {record.ExportRecordId}.", emitArchitectureDocxExportGeneratedAudit: false, cancellationToken);
        recordedReplayExportRecordId = persisted.ExportRecordId;
        return new ReplayExportResult
        {
            ExportRecordId = record.ExportRecordId,
            RecordedReplayExportRecordId = recordedReplayExportRecordId,
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
    ///     Appends <c>_replay</c> to the base file name while preserving the original extension.
    ///     Returns <c>replayed_export.docx</c> when the original name is blank.
    /// </summary>
    private static string BuildReplayFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return FallbackReplayFileName;
        string extension = Path.GetExtension(originalFileName);
        string baseName = Path.GetFileNameWithoutExtension(originalFileName);
        return $"{baseName}_replay{extension}";
    }
}
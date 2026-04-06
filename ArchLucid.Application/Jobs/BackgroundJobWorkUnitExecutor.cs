using ArchiForge.Application.Analysis;
using ArchiForge.Contracts.Architecture;

namespace ArchiForge.Application.Jobs;

/// <summary>
/// Loads run detail from persistence and runs DOCX export pipelines for queued work units.
/// </summary>
public sealed class BackgroundJobWorkUnitExecutor(
    IRunDetailQueryService runDetailQuery,
    IArchitectureAnalysisService architectureAnalysisService,
    IArchitectureAnalysisDocxExportService docxExportService,
    IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService) : IBackgroundJobWorkUnitExecutor
{
    public async Task<BackgroundJobFile> ExecuteAsync(BackgroundJobWorkUnit workUnit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workUnit);

        return workUnit switch
        {
            AnalysisReportDocxWorkUnit w => await ExecuteAnalysisReportDocxAsync(w, cancellationToken),
            ConsultingDocxWorkUnit w => await ExecuteConsultingDocxAsync(w, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported background job work unit: {workUnit.GetType().Name}.")
        };
    }

    private async Task<BackgroundJobFile> ExecuteAnalysisReportDocxAsync(
        AnalysisReportDocxWorkUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit.Payload);

        ArchitectureAnalysisRequest request = unit.Payload.ToAnalysisRequest();
        ArchitectureRunDetail? detail =
            await runDetailQuery.GetRunDetailAsync(unit.Payload.RunId, cancellationToken);

        if (detail is null)
            throw new InvalidOperationException($"Run '{unit.Payload.RunId}' was not found.");

        request.PreloadedRunDetail = detail;

        byte[] bytes = await docxExportService.GenerateDocxAsync(
            await architectureAnalysisService.BuildAsync(request, cancellationToken),
            cancellationToken);

        return new BackgroundJobFile(unit.FileName, unit.ContentType, bytes);
    }

    private async Task<BackgroundJobFile> ExecuteConsultingDocxAsync(
        ConsultingDocxWorkUnit unit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit.Payload);

        ConsultingDocxJobPayload p = unit.Payload;

        ArchitectureAnalysisRequest analysisRequest = new()
        {
            RunId = p.RunId,
            IncludeEvidence = p.IncludeEvidence,
            IncludeExecutionTraces = p.IncludeExecutionTraces,
            IncludeManifest = p.IncludeManifest,
            IncludeDiagram = p.IncludeDiagram,
            IncludeSummary = true,
            IncludeDeterminismCheck = p.IncludeDeterminismCheck,
            DeterminismIterations = p.DeterminismIterations,
            IncludeManifestCompare = p.IncludeManifestCompare,
            CompareManifestVersion = p.CompareManifestVersion,
            IncludeAgentResultCompare = p.IncludeAgentResultCompare,
            CompareRunId = p.CompareRunId
        };

        ArchitectureRunDetail? detail = await runDetailQuery.GetRunDetailAsync(p.RunId, cancellationToken);

        if (detail is null)
            throw new InvalidOperationException($"Run '{p.RunId}' was not found.");

        analysisRequest.PreloadedRunDetail = detail;

        ArchitectureAnalysisReport report =
            await architectureAnalysisService.BuildAsync(analysisRequest, cancellationToken);

        byte[] bytes = await consultingDocxExportService.GenerateDocxAsync(report, cancellationToken);

        return new BackgroundJobFile(unit.FileName, unit.ContentType, bytes);
    }
}

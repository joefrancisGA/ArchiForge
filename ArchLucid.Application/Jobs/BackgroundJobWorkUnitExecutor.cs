using System.Text.Json;
using ArchLucid.Application.Analysis;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Application.Jobs;
/// <summary>
///     Loads run detail from persistence and runs DOCX export pipelines for queued work units.
/// </summary>
public sealed class BackgroundJobWorkUnitExecutor(IRunDetailQueryService runDetailQuery, IArchitectureAnalysisService architectureAnalysisService, IArchitectureAnalysisDocxExportService docxExportService, IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService, IAuditService auditService) : IBackgroundJobWorkUnitExecutor
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runDetailQuery, architectureAnalysisService, docxExportService, consultingDocxExportService, auditService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.IRunDetailQueryService runDetailQuery, ArchLucid.Application.Analysis.IArchitectureAnalysisService architectureAnalysisService, ArchLucid.Application.Analysis.IArchitectureAnalysisDocxExportService docxExportService, ArchLucid.Application.Analysis.IArchitectureAnalysisConsultingDocxExportService consultingDocxExportService, ArchLucid.Core.Audit.IAuditService auditService)
    {
        ArgumentNullException.ThrowIfNull(runDetailQuery);
        ArgumentNullException.ThrowIfNull(architectureAnalysisService);
        ArgumentNullException.ThrowIfNull(docxExportService);
        ArgumentNullException.ThrowIfNull(consultingDocxExportService);
        ArgumentNullException.ThrowIfNull(auditService);
        return (byte)0;
    }

    private readonly IRunDetailQueryService _runDetailQuery = runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));
    private readonly IArchitectureAnalysisService _architectureAnalysisService = architectureAnalysisService ?? throw new ArgumentNullException(nameof(architectureAnalysisService));
    private readonly IArchitectureAnalysisDocxExportService _docxExportService = docxExportService ?? throw new ArgumentNullException(nameof(docxExportService));
    private readonly IArchitectureAnalysisConsultingDocxExportService _consultingDocxExportService = consultingDocxExportService ?? throw new ArgumentNullException(nameof(consultingDocxExportService));
    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    public async Task<BackgroundJobFile> ExecuteAsync(BackgroundJobWorkUnit workUnit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workUnit);
        return workUnit switch
        {
            AnalysisReportDocxWorkUnit w => await ExecuteAnalysisReportDocxAsync(w, cancellationToken),
            ConsultingDocxWorkUnit w => await ExecuteConsultingDocxAsync(w, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported background job work unit: {workUnit.GetType().Name}.")};
    }

    private async Task<BackgroundJobFile> ExecuteAnalysisReportDocxAsync(AnalysisReportDocxWorkUnit unit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unit.Payload);
        ArchitectureAnalysisRequest request = unit.Payload.ToAnalysisRequest();
        ArchitectureRunDetail? detail = await _runDetailQuery.GetRunDetailAsync(unit.Payload.RunId, cancellationToken);
        if (detail is null)
            throw new InvalidOperationException($"Run '{unit.Payload.RunId}' was not found.");
        request.PreloadedRunDetail = detail;
        byte[] bytes = await _docxExportService.GenerateDocxAsync(await _architectureAnalysisService.BuildAsync(request, cancellationToken), cancellationToken);
        await LogArchitectureDocxExportGeneratedAsync(unit.Payload.RunId, "analysis-report-docx-async", bytes.Length, unit.FileName, cancellationToken);
        return new BackgroundJobFile(unit.FileName, unit.ContentType, bytes);
    }

    private async Task<BackgroundJobFile> ExecuteConsultingDocxAsync(ConsultingDocxWorkUnit unit, CancellationToken cancellationToken)
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
        ArchitectureRunDetail? detail = await _runDetailQuery.GetRunDetailAsync(p.RunId, cancellationToken);
        if (detail is null)
            throw new InvalidOperationException($"Run '{p.RunId}' was not found.");
        analysisRequest.PreloadedRunDetail = detail;
        ArchitectureAnalysisReport report = await _architectureAnalysisService.BuildAsync(analysisRequest, cancellationToken);
        byte[] bytes = await _consultingDocxExportService.GenerateDocxAsync(report, cancellationToken);
        await LogArchitectureDocxExportGeneratedAsync(p.RunId, "analysis-report-consulting-docx-async", bytes.Length, unit.FileName, cancellationToken);
        return new BackgroundJobFile(unit.FileName, unit.ContentType, bytes);
    }

    private async Task LogArchitectureDocxExportGeneratedAsync(string runId, string exportChannel, int byteCount, string fileName, CancellationToken cancellationToken)
    {
        Guid correlationSuffix = Guid.NewGuid();
        DateTime occurredUtc = DateTime.UtcNow;
        Guid? auditRunId = TryParseRunGuid(runId);
        await _auditService.LogAsync(new AuditEvent { OccurredUtc = occurredUtc, EventType = AuditEventTypes.ArchitectureDocxExportGenerated, CorrelationId = $"{exportChannel}:{runId}:{correlationSuffix:N}", RunId = auditRunId, DataJson = JsonSerializer.Serialize(new { runId, exportChannel, byteCount, fileName }, AuditJsonSerializationOptions.Instance) }, cancellationToken);
    }

    private static Guid? TryParseRunGuid(string runId)
    {
        if (Guid.TryParseExact(runId, "N", out Guid guid))
            return guid;
        if (Guid.TryParse(runId, out guid))
            return guid;
        return null;
    }
}
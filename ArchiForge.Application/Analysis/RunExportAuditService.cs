using System.Text.Json;

using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class RunExportAuditService(IRunExportRecordRepository repository) : IRunExportAuditService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<RunExportRecord> RecordAsync(
        string runId,
        string exportType,
        string format,
        string fileName,
        string? templateProfile,
        string? templateProfileDisplayName,
        bool wasAutoSelected,
        string? resolutionReason,
        string? manifestVersion,
        PersistedAnalysisExportRequest? analysisRequest = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportType);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        RunExportRecord record = new RunExportRecord
        {
            ExportRecordId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            ExportType = exportType,
            Format = format,
            FileName = fileName,
            TemplateProfile = templateProfile,
            TemplateProfileDisplayName = templateProfileDisplayName,
            WasAutoSelected = wasAutoSelected,
            ResolutionReason = resolutionReason,
            ManifestVersion = manifestVersion,
            Notes = notes,
            AnalysisRequestJson = analysisRequest is null
                ? null
                : JsonSerializer.Serialize(analysisRequest, _jsonOptions),
            IncludedEvidence = analysisRequest?.IncludeEvidence,
            IncludedExecutionTraces = analysisRequest?.IncludeExecutionTraces,
            IncludedManifest = analysisRequest?.IncludeManifest,
            IncludedDiagram = analysisRequest?.IncludeDiagram,
            IncludedSummary = analysisRequest?.IncludeSummary,
            IncludedDeterminismCheck = analysisRequest?.IncludeDeterminismCheck,
            DeterminismIterations = analysisRequest?.DeterminismIterations,
            IncludedManifestCompare = analysisRequest?.IncludeManifestCompare,
            CompareManifestVersion = analysisRequest?.CompareManifestVersion,
            IncludedAgentResultCompare = analysisRequest?.IncludeAgentResultCompare,
            CompareRunId = analysisRequest?.CompareRunId,
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(record, cancellationToken);

        return record;
    }
}


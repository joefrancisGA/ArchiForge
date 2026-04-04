using System.Text.Json;

using ArchiForge.Contracts.Metadata;
using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Records export operations against a run to the <see cref="IRunExportRecordRepository"/>.
/// Captures the full export configuration (template profile, included sections, determinism options)
/// so that exports can be audited, replayed, and diffed after the fact.
/// </summary>
public sealed class RunExportAuditService(IRunExportRecordRepository repository) : IRunExportAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
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

        RunExportRecord record = new()
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
                : JsonSerializer.Serialize(analysisRequest, JsonOptions),
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


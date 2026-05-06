using System.Text.Json;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Records export operations against a run to the <see cref = "IRunExportRecordRepository"/>.
///     Captures the full export configuration (template profile, included sections, determinism options)
///     so that exports can be audited, replayed, and diffed after the fact.
/// </summary>
public sealed class RunExportAuditService(IRunExportRecordRepository repository, IAuditService auditService) : IRunExportAuditService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(repository, auditService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Data.Repositories.IRunExportRecordRepository repository, ArchLucid.Core.Audit.IAuditService auditService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(auditService);
        return (byte)0;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly IRunExportRecordRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    public async Task<RunExportRecord> RecordAsync(string runId, string exportType, string format, string fileName, string? templateProfile, string? templateProfileDisplayName, bool wasAutoSelected, string? resolutionReason, string? manifestVersion, PersistedAnalysisExportRequest? analysisRequest = null, string? notes = null, bool emitArchitectureDocxExportGeneratedAudit = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentNullException.ThrowIfNull(exportType);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(fileName);
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
            AnalysisRequestJson = analysisRequest is null ? null : JsonSerializer.Serialize(analysisRequest, JsonOptions),
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
        await _repository.CreateAsync(record, cancellationToken);
        if (emitArchitectureDocxExportGeneratedAudit)
        {
            Guid? auditRunId = TryParseRunGuid(runId);
            DateTime occurredUtc = DateTime.UtcNow;
            await _auditService.LogAsync(new AuditEvent { OccurredUtc = occurredUtc, EventType = AuditEventTypes.ArchitectureDocxExportGenerated, RunId = auditRunId, DataJson = JsonSerializer.Serialize(new { runId, exportRecordId = record.ExportRecordId, exportType = record.ExportType, fileName = record.FileName, templateProfile = record.TemplateProfile, manifestVersion = record.ManifestVersion, compareWithRunId = analysisRequest?.CompareRunId }, AuditJsonSerializationOptions.Instance) }, cancellationToken);
        }

        return record;
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
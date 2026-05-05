using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

public interface IRunExportAuditService
{
    Task<RunExportRecord> RecordAsync(
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
        bool emitArchitectureDocxExportGeneratedAudit = true,
        CancellationToken cancellationToken = default);
}

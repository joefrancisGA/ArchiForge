using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

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
        string? notes = null,
        CancellationToken cancellationToken = default);
}


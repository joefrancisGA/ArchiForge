using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Analysis;

public sealed class RunExportAuditService : IRunExportAuditService
{
    private readonly IRunExportRecordRepository _repository;

    public RunExportAuditService(IRunExportRecordRepository repository)
    {
        _repository = repository;
    }

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
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var record = new RunExportRecord
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
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(record, cancellationToken);

        return record;
    }
}


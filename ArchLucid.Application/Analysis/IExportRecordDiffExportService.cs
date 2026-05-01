namespace ArchLucid.Application.Analysis;

public interface IExportRecordDiffExportService
{
    Task<byte[]> GenerateDocxAsync(
        ExportRecordDiffResult diff,
        CancellationToken cancellationToken = default);
}

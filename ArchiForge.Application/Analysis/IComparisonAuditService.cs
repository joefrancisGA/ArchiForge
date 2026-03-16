namespace ArchiForge.Application.Analysis;

public interface IComparisonAuditService
{
    Task<string> RecordEndToEndAsync(
        EndToEndReplayComparisonReport report,
        string summaryMarkdown,
        CancellationToken cancellationToken = default);

    Task<string> RecordExportDiffAsync(
        ExportRecordDiffResult diff,
        string summaryMarkdown,
        CancellationToken cancellationToken = default);
}


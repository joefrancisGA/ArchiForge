namespace ArchLucid.Application.Analysis;

public interface IExportReplayService
{
    Task<ReplayExportResult> ReplayAsync(
        ReplayExportRequest request,
        CancellationToken cancellationToken = default);
}

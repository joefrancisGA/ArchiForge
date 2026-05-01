namespace ArchLucid.Application.Analysis;

public interface IEndToEndReplayComparisonExportService
{
    string GenerateMarkdown(EndToEndReplayComparisonReport report, string? profile = null);

    string GenerateHtml(EndToEndReplayComparisonReport report, string? profile = null);

    Task<byte[]> GenerateDocxAsync(
        EndToEndReplayComparisonReport report,
        CancellationToken cancellationToken = default,
        string? profile = null);

    Task<byte[]> GeneratePdfAsync(
        EndToEndReplayComparisonReport report,
        CancellationToken cancellationToken = default,
        string? profile = null);
}

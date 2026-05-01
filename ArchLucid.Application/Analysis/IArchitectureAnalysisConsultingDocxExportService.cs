namespace ArchLucid.Application.Analysis;

public interface IArchitectureAnalysisConsultingDocxExportService
{
    Task<byte[]> GenerateDocxAsync(
        ArchitectureAnalysisReport report,
        CancellationToken cancellationToken = default);
}

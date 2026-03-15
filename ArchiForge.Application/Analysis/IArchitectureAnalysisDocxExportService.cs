namespace ArchiForge.Application.Analysis;

public interface IArchitectureAnalysisDocxExportService
{
    byte[] GenerateDocx(ArchitectureAnalysisReport report);
}

namespace ArchiForge.Application.Analysis;

public interface IArchitectureAnalysisExportService
{
    string GenerateMarkdown(ArchitectureAnalysisReport report);
}

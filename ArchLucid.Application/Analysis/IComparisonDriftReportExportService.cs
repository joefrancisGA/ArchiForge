namespace ArchiForge.Application.Analysis;

public interface IComparisonDriftReportExportService
{
    string GenerateMarkdown(DriftAnalysisResult drift, string? comparisonRecordId = null);

    byte[] GenerateDocx(DriftAnalysisResult drift, string? comparisonRecordId = null);
}


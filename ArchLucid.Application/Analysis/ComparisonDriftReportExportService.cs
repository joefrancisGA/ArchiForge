using System.Text;

namespace ArchLucid.Application.Analysis;

public sealed class ComparisonDriftReportExportService : IComparisonDriftReportExportService
{
    private readonly DriftReportDocxExport _docx = new();

    public string GenerateMarkdown(DriftAnalysisResult drift, string? comparisonRecordId = null)
    {
        ArgumentNullException.ThrowIfNull(drift);

        StringBuilder sb = new();
        sb.AppendLine("# ArchLucid Comparison Drift Report");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(comparisonRecordId))
        
            sb.AppendLine($"- **Comparison record**: `{comparisonRecordId}`");
        
        sb.AppendLine($"- **Drift detected**: {(drift.DriftDetected ? "Yes" : "No")}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(drift.Summary))
        {
            sb.AppendLine(drift.Summary.Trim());
            sb.AppendLine();
        }

        if (drift.Items.Count <= 0)
            return sb.ToString();

        sb.AppendLine("## Differences");
        sb.AppendLine();

        foreach (DriftItem item in drift.Items)
        {
            sb.AppendLine($"- **{item.Category}** — `{item.Path}`");
            if (!string.IsNullOrWhiteSpace(item.Description))
            
                sb.AppendLine($"  - {item.Description}");
            
            if (item.StoredValue is not null)
            
                sb.AppendLine($"  - Stored: `{item.StoredValue}`");
            
            if (item.RegeneratedValue is not null)
            
                sb.AppendLine($"  - Regenerated: `{item.RegeneratedValue}`");
            
        }

        return sb.ToString();
    }

    public byte[] GenerateDocx(DriftAnalysisResult drift, string? comparisonRecordId = null) =>
        _docx.GenerateDocx(drift, comparisonRecordId);
}


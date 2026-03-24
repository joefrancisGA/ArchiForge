using System.Text;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Formats a <see cref="DriftAnalysisResult"/> as either a GitHub-flavoured Markdown
/// document or an HTML page.
/// </summary>
/// <remarks>
/// Implements <see cref="IDriftReportFormatter"/> and is the default formatter registered
/// in the DI container for drift-report exports.
/// </remarks>
public sealed class MarkdownDriftReportFormatter : IDriftReportFormatter
{
    /// <inheritdoc />
    public string FormatMarkdown(DriftAnalysisResult drift, string? comparisonRecordId = null)
    {
        ArgumentNullException.ThrowIfNull(drift);

        var sb = new StringBuilder();
        sb.AppendLine("# ArchiForge Comparison Drift Report");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(comparisonRecordId))
        {
            sb.AppendLine($"**Comparison record:** `{comparisonRecordId}`");
            sb.AppendLine();
        }
        sb.AppendLine($"**Drift detected:** {(drift.DriftDetected ? "Yes" : "No")}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(drift.Summary))
        {
            sb.AppendLine(drift.Summary);
            sb.AppendLine();
        }
        if (drift.Items.Count > 0)
        {
            sb.AppendLine("## Differences");
            sb.AppendLine();
            sb.AppendLine("| Category | Path | Stored | Regenerated | Description |");
            sb.AppendLine("|----------|------|--------|-------------|-------------|");
            foreach (var item in drift.Items)
            {
                var stored = EscapeTableCell(item.StoredValue);
                var regen = EscapeTableCell(item.RegeneratedValue);
                var desc = EscapeTableCell(item.Description);
                sb.AppendLine($"| {item.Category} | {item.Path} | {stored} | {regen} | {desc} |");
            }
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatHtml(DriftAnalysisResult drift, string? comparisonRecordId = null)
    {
        ArgumentNullException.ThrowIfNull(drift);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>ArchiForge Drift Report</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;margin:1rem;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #ccc;padding:0.5rem;text-align:left;} th{background:#eee;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>ArchiForge Comparison Drift Report</h1>");
        if (!string.IsNullOrWhiteSpace(comparisonRecordId))
            sb.AppendLine($"<p><strong>Comparison record:</strong> <code>{System.Net.WebUtility.HtmlEncode(comparisonRecordId)}</code></p>");
        sb.AppendLine($"<p><strong>Drift detected:</strong> {(drift.DriftDetected ? "Yes" : "No")}</p>");
        if (!string.IsNullOrWhiteSpace(drift.Summary))
            sb.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(drift.Summary)}</p>");
        if (drift.Items.Count > 0)
        {
            sb.AppendLine("<h2>Differences</h2><table><thead><tr><th>Category</th><th>Path</th><th>Stored</th><th>Regenerated</th><th>Description</th></tr></thead><tbody>");
            foreach (var item in drift.Items)
            {
                sb.Append("<tr><td>").Append(System.Net.WebUtility.HtmlEncode(item.Category))
                    .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.Path))
                    .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.StoredValue ?? ""))
                    .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.RegeneratedValue ?? ""))
                    .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.Description))
                    .AppendLine("</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a Markdown table cell value to prevent pipe characters and newlines
    /// from breaking the table layout.
    /// </summary>
    private static string EscapeTableCell(string? value)
    {
        if (value is null)
            return "";
        return value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", "").Replace("\n", " ");
    }
}

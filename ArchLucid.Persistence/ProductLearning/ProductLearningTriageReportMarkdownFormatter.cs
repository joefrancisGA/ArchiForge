using System.Text;

using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>Human-readable markdown for architecture/product triage discussions (58R).</summary>
public static class ProductLearningTriageReportMarkdownFormatter
{
    public static string Format(ProductLearningTriageReportDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        StringBuilder sb = new();

        sb.AppendLine("# Pilot feedback — triage summary");
        sb.AppendLine();
        sb.AppendLine("Concise rollups for review. Raw pilot comments are not included.");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine("- **Tenant:** `" + doc.TenantId + "`");
        sb.AppendLine("- **Workspace:** `" + doc.WorkspaceId + "`");
        sb.AppendLine("- **Project:** `" + doc.ProjectId + "`");
        sb.AppendLine(
            "- **Generated (UTC):** " + doc.GeneratedUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z");

        if (doc.SinceUtc is DateTime since)
        
            sb.AppendLine(
                "- **Signals since (UTC):** " + since.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z");
        
        else
        
            sb.AppendLine("- **Signals since:** all time");
        

        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("| --- | ---: |");
        sb.AppendLine("| Total signals in scope | " + doc.TotalSignalsInScope + " |");
        sb.AppendLine("| Runs with feedback | " + doc.DistinctRunsReviewed + " |");
        sb.AppendLine();

        sb.AppendLine("## Artifact outcomes (trusted / revised / rejected / follow-up)");
        sb.AppendLine();

        if (doc.ArtifactOutcomes.Count == 0)
        {
            sb.AppendLine("*No artifact trend rows met the dashboard thresholds for this window.*");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("| Area | Trusted | Revised | Rejected | Follow-up | Runs | Hint |");
            sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");

            foreach (ProductLearningTriageReportArtifactRow row in doc.ArtifactOutcomes)
            
                sb.AppendLine(
                    "| "
                    + MdCell(row.ArtifactLabel)
                    + " | "
                    + row.Trusted
                    + " | "
                    + row.Revised
                    + " | "
                    + row.Rejected
                    + " | "
                    + row.FollowUp
                    + " | "
                    + row.Runs
                    + " | "
                    + MdCell(row.ThemeHint ?? "—")
                    + " |");
            

            sb.AppendLine();
        }

        sb.AppendLine("## Top repeated problem areas");
        sb.AppendLine();

        if (doc.TopProblemAreas.Count == 0)
        {
            sb.AppendLine("*None surfaced above noise gates.*");
            sb.AppendLine();
        }
        else
        {
            int n = 1;

            foreach (string line in doc.TopProblemAreas)
            {
                sb.AppendLine(n + ". " + line);
                n++;
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Suggested product improvements");
        sb.AppendLine();

        if (doc.TopImprovements.Count == 0)
        {
            sb.AppendLine("*No ranked opportunities in this window.*");
            sb.AppendLine();
        }
        else
        {
            int n = 1;

            foreach (ProductLearningTriageReportImprovementLine o in doc.TopImprovements)
            {
                sb.AppendLine(
                    n
                    + ". **"
                    + EscapeBoldFragment(o.Title)
                    + "** ("
                    + MdPlain(o.Severity)
                    + ") — "
                    + o.Area
                    + ": "
                    + o.Summary);
                n++;
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Triage queue (next to review)");
        sb.AppendLine();

        if (doc.TriageQueuePreview.Count == 0)
        {
            sb.AppendLine("*Queue empty for this scope and window.*");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("| # | Title | Severity | Detail | Next step |");
            sb.AppendLine("| ---: | --- | --- | --- | --- |");

            foreach (ProductLearningTriageReportTriageLine q in doc.TriageQueuePreview)
            
                sb.AppendLine(
                    "| "
                    + q.Rank
                    + " | "
                    + MdCell(q.Title)
                    + " | "
                    + MdCell(q.Severity)
                    + " | "
                    + MdCell(q.DetailSummary)
                    + " | "
                    + MdCell(q.SuggestedNextStep ?? "—")
                    + " |");
            

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string MdCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        
            return "—";
        

        string s = value.Replace("\r\n", " ", StringComparison.Ordinal);
        s = s.Replace("\n", " ", StringComparison.Ordinal);
        s = s.Replace("|", "·", StringComparison.Ordinal).Trim();

        return s.Length == 0 ? "—" : s;
    }

    private static string MdPlain(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string EscapeBoldFragment(string value)
    {
        return value.Replace("**", "·", StringComparison.Ordinal).Trim();
    }
}

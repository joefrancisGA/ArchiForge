using System.Globalization;
using System.Text;

namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Formats golden-cohort drift output as Markdown for CI / <c>docs/quality</c> consumers.</summary>
public static class GoldenCohortDriftMarkdown
{
    public static string BuildReport(
        DateTimeOffset generatedUtc,
        IReadOnlyList<GoldenCohortDriftRow> rows,
        string preamble)
    {
        StringBuilder sb = new();
        sb.AppendLine("# Golden cohort drift report");
        sb.AppendLine();
        sb.AppendLine($"**Generated (UTC):** {generatedUtc:O}");
        sb.AppendLine();
        sb.AppendLine(preamble.Trim());
        sb.AppendLine();

        if (rows.Count == 0)
        {
            sb.AppendLine("_No rows evaluated._");

            return sb.ToString();
        }

        sb.AppendLine("| Item | Expected SHA | Actual SHA | SHA match | Expected categories | Actual categories | Category match |");
        sb.AppendLine("|------|--------------|------------|-----------|---------------------|-------------------|----------------|");

        foreach (GoldenCohortDriftRow row in rows)
        {
            sb.AppendLine(
                $"| {Escape(row.ItemId)} | `{Escape(row.ExpectedSha)}` | `{Escape(row.ActualSha)}` | {row.ShaMatches} | "
                + $"{Escape(row.ExpectedCategories)} | {Escape(row.ActualCategories)} | {row.CategoryMatches} |");
        }

        sb.AppendLine();

        int mismatches = rows.Count(r => !r.ShaMatches || !r.CategoryMatches);
        sb.AppendLine(
            $"**Summary:** {mismatches.ToString(CultureInfo.InvariantCulture)} / {rows.Count.ToString(CultureInfo.InvariantCulture)} items drifted.");

        return sb.ToString();
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}

/// <summary>One evaluated cohort row for drift reporting.</summary>
public sealed record GoldenCohortDriftRow(
    string ItemId,
    string ExpectedSha,
    string ActualSha,
    bool ShaMatches,
    string ExpectedCategories,
    string ActualCategories,
    bool CategoryMatches);

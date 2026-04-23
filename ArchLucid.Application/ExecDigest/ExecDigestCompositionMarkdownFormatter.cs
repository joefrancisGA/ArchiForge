using System.Globalization;
using System.Text;

using ArchLucid.Application.Value;

namespace ArchLucid.Application.ExecDigest;

/// <summary>
///     Markdown projection of <see cref="ExecDigestComposition" /> for board-pack PDF reuse (same fields as weekly
///     email model).
/// </summary>
public static class ExecDigestCompositionMarkdownFormatter
{
    /// <summary>
    ///     Formats digest highlights without duplicating ROI math (value section comes from
    ///     <see cref="ValueReportBuilder" />).
    /// </summary>
    public static string Format(ExecDigestComposition composition)
    {
        if (composition is null)
            throw new ArgumentNullException(nameof(composition));

        StringBuilder sb = new();
        sb.AppendLine("# Executive digest highlights (weekly pipeline)");
        sb.AppendLine();
        sb.AppendLine($"**Week label:** {composition.WeekLabel}");
        sb.AppendLine($"**Dashboard:** {composition.DashboardUrl}");
        sb.AppendLine($"**Sponsor value link:** {composition.SponsorValueReportUrl}");
        sb.AppendLine();

        if (composition.CommittedManifestsInWeek is { } c)
            sb.AppendLine($"**Committed manifests (digest window):** {c.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(composition.FindingsDeltaSummary))
        {
            sb.AppendLine();
            sb.AppendLine("## Findings delta");
            sb.AppendLine(composition.FindingsDeltaSummary);
        }

        if (composition.TopManifestRuns is { Count: > 0 } runs)
        {
            sb.AppendLine();
            sb.AppendLine("## Highlighted runs");
            foreach (ExecDigestHighlightedRun run in runs)
            {
                sb.AppendLine(
                    $"- `{run.RunIdHex}` — score {run.SignificanceScore.ToString(CultureInfo.InvariantCulture)}"
                    + (string.IsNullOrWhiteSpace(run.Caption) ? string.Empty : $" — {run.Caption}"));
            }
        }

        if (string.IsNullOrWhiteSpace(composition.ComplianceDriftMarkdown))
            return sb.ToString();

        sb.AppendLine();
        sb.AppendLine("## Compliance drift");
        sb.AppendLine(composition.ComplianceDriftMarkdown);

        return sb.ToString();
    }
}

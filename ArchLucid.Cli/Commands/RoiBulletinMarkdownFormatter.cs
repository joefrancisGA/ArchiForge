using System.Globalization;

namespace ArchLucid.Cli.Commands;

/// <summary>Builds a Markdown draft that mirrors <c>docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md</c> fields.</summary>
internal static class RoiBulletinMarkdownFormatter
{
    internal static string FormatDraft(RoiBulletinPreviewPayload payload, int minTenantsUsed)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        string quarter = string.IsNullOrWhiteSpace(payload.Quarter) ? "(unknown)" : payload.Quarter;

        return string.Join(
            Environment.NewLine,
            [
                $"# Aggregate ROI bulletin — draft ({quarter})",
                string.Empty,
                "> **DRAFT — NOT FOR EXTERNAL PUBLICATION.** Owner sign-off required per `docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md`.",
                string.Empty,
                "## Sample",
                $"- **Tenants included (tenant-supplied baseline, quarter window):** {payload.TenantCount.ToString(CultureInfo.InvariantCulture)}",
                $"- **Minimum-N gate used for this CLI run:** {minTenantsUsed.ToString(CultureInfo.InvariantCulture)}",
                string.Empty,
                "## Headline statistics (baseline review-cycle hours, tenant-supplied only)",
                $"- **Mean:** {FormatHours(payload.MeanBaselineHours)} h",
                $"- **Median (p50):** {FormatHours(payload.MedianBaselineHours)} h",
                $"- **p90:** {FormatHours(payload.P90BaselineHours)} h",
                string.Empty,
                "## Next steps",
                "- Paste into the quarterly bulletin workflow after legal/comms review.",
                "- Never attach per-tenant rows; this draft is aggregate-only.",
                string.Empty,
            ]);
    }

    private static string FormatHours(decimal? hours) =>
        hours is { } h ? h.ToString("0.##", CultureInfo.InvariantCulture) : "—";
}

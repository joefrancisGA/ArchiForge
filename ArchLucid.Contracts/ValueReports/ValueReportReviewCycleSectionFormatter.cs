using System.Globalization;
using System.Text;

namespace ArchLucid.Contracts.ValueReports;

/// <summary>
///     Shared copy for the "Review-cycle delta" block in DOCX and Markdown (value-report and first-value report).
/// </summary>
public static class ValueReportReviewCycleSectionFormatter
{
    private const string Heading = "Review-cycle delta (before vs measured)";

    /// <summary>Paragraphs for OpenXML (font sizes match existing value-report sections).</summary>
    public static IReadOnlyList<ValueReportReviewCycleParagraph> GetParagraphs(ValueReportSnapshot snapshot)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        List<ValueReportReviewCycleParagraph> list =
        [
            new(Heading, true, false, 28)
        ];

        if (snapshot.ReviewCycleBaselineProvenance is ReviewCycleBaselineProvenance.NoMeasurementYet)
        {
            list.Add(
                new ValueReportReviewCycleParagraph(
                    "(No committed manifests in this window — measured review cycle is not yet available.)",
                    false,
                    true,
                    22));

            return list;
        }

        decimal? effectiveBaseline = ComputeEffectiveBaselineHours(snapshot);

        string provenanceLabel = snapshot.ReviewCycleBaselineProvenance switch
        {
            ReviewCycleBaselineProvenance.TenantSuppliedAtSignup => "tenant-supplied at trial signup",
            ReviewCycleBaselineProvenance.TenantSuppliedViaSettings => "tenant-supplied via baseline (settings) page",
            ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions =>
                "default from PILOT_ROI_MODEL.md (tenant did not provide a baseline at signup)",
            _ => string.Empty
        };

        list.Add(
            new ValueReportReviewCycleParagraph(
                $"Baseline review cycle: {FormatHours(effectiveBaseline)} h ({provenanceLabel})",
                false,
                false,
                22));

        decimal? measured = snapshot.MeasuredAverageReviewCycleHoursForWindow;
        int sampleSize = snapshot.MeasuredReviewCycleSampleSize;

        list.Add(
            new ValueReportReviewCycleParagraph(
                $"Measured review cycle (this window): {FormatHours(measured)} h across {sampleSize.ToString(CultureInfo.InvariantCulture)} run(s)",
                false,
                false,
                22));

        decimal? delta = snapshot.ReviewCycleHoursDelta;
        decimal? deltaPct = snapshot.ReviewCycleHoursDeltaPercent;

        string deltaLine = deltaPct is { } pct
            ? $"Delta: {FormatHours(delta)} h saved per run ({pct.ToString("0.##", CultureInfo.InvariantCulture)}% improvement)"
            : $"Delta: {FormatHours(delta)} h saved per run";

        list.Add(new ValueReportReviewCycleParagraph(deltaLine, false, false, 22));

        if (snapshot.ReviewCycleBaselineProvenance is ReviewCycleBaselineProvenance.TenantSuppliedAtSignup
            or ReviewCycleBaselineProvenance.TenantSuppliedViaSettings)
        {
            if (snapshot.TenantBaselineReviewCycleCapturedUtc is { } captured)
            {
                string when =
                    snapshot.ReviewCycleBaselineProvenance is ReviewCycleBaselineProvenance.TenantSuppliedViaSettings
                        ? "Captured in baseline settings (UTC)"
                        : "Captured at signup (UTC)";

                list.Add(
                    new ValueReportReviewCycleParagraph(
                        $"{when}: {captured.ToString("O", CultureInfo.InvariantCulture)}",
                        false,
                        true,
                        22));
            }

            if (!string.IsNullOrWhiteSpace(snapshot.TenantBaselineReviewCycleSource) &&
                !string.Equals(
                    snapshot.TenantBaselineReviewCycleSource.Trim(),
                    "baseline_settings",
                    StringComparison.Ordinal))
            {
                list.Add(
                    new ValueReportReviewCycleParagraph(
                        $"Source note: {snapshot.TenantBaselineReviewCycleSource.Trim()}",
                        false,
                        true,
                        22));
            }
        }

        if (snapshot.ReviewCycleBaselineProvenance is ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions)
        {
            list.Add(
                new ValueReportReviewCycleParagraph(
                    "This delta uses the conservative default from PILOT_ROI_MODEL.md because the prospect did not supply a baseline at signup. Numbers are illustrative, not customer-specific.",
                    false,
                    true,
                    22));
        }

        return list;
    }

    /// <summary>Appends the Markdown section (## heading + lines). Caller supplies leading blank lines if needed.</summary>
    public static void AppendMarkdownSection(StringBuilder sb, ValueReportSnapshot snapshot)
    {
        if (sb is null)
            throw new ArgumentNullException(nameof(sb));
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        IReadOnlyList<ValueReportReviewCycleParagraph> paragraphs = GetParagraphs(snapshot);

        sb.AppendLine("## Review-cycle delta (before vs measured)");
        sb.AppendLine();

        foreach (ValueReportReviewCycleParagraph p in paragraphs)
        {
            if (string.Equals(p.Text, Heading, StringComparison.Ordinal))
            {
                continue;
            }

            if (p.Italic)
            {
                sb.AppendLine($"_{p.Text}_");
                sb.AppendLine();

                continue;
            }

            sb.AppendLine(p.Text);
            sb.AppendLine();
        }
    }

    private static decimal? ComputeEffectiveBaselineHours(ValueReportSnapshot snapshot)
    {
        if (snapshot is { MeasuredAverageReviewCycleHoursForWindow: { } m, ReviewCycleHoursDelta: { } d })
            return m + d;

        return snapshot.TenantBaselineReviewCycleHours;
    }

    private static string FormatHours(decimal? hours)
    {
        return hours is { } h ? h.ToString("0.##", CultureInfo.InvariantCulture) : "0";
    }
}

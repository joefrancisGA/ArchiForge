using System.Globalization;
using System.Text;
using ArchLucid.Contracts.ValueReports;

namespace ArchLucid.Application.Pilots;
/// <summary>ROI evidence-confidence block appended to sponsor first-value-report Markdown alongside baseline tables.</summary>
public static class RoiEvidenceCompletenessMarkdownFormatter
{
    /// <summary>Appends a conservative sponsor-facing completeness section derived from persisted tenant ROI baseline posture.</summary>
    public static void AppendMarkdownSection(StringBuilder sb, ValueReportSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(snapshot);
        (string headline, string body) = Describe(snapshot);
        sb.AppendLine("## ROI evidence completeness");
        sb.AppendLine();
        sb.AppendLine("**Indicator:** Quantitative deltas use ArchLucid-persisted run facts; comparative dollar narratives inherit baseline posture captured for this tenant. Summarizes **confidence** — not a financial attestation.");
        sb.AppendLine();
        sb.AppendLine($"**Status:** **{headline}**");
        sb.AppendLine();
        sb.AppendLine(body);
        sb.AppendLine();
    }

    internal static (string Headline, string Body) Describe(ValueReportSnapshot snapshot) => snapshot.ReviewCycleBaselineProvenance switch
    {
        ReviewCycleBaselineProvenance.TenantSuppliedAtSignup => DescribeTenantCaptured(snapshot, "Strong", "Tenant supplied baseline review-cycle hours at signup"),
        ReviewCycleBaselineProvenance.TenantSuppliedViaSettings => DescribeTenantCaptured(snapshot, "Strong", "Tenant maintained baseline inputs via baseline settings"),
        ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions => ("Partial", "Baseline hours **default from repository ROI model assumptions** (`docs/library/PILOT_ROI_MODEL.md`). " + "**Do not quote customer-specific savings** without tenant-captured baselines."),
        ReviewCycleBaselineProvenance.NoMeasurementYet or _ => ("Low confidence", "No tenant baseline measurements were captured for this cohort window; treat ROI tables as " + "**illustrative / internal planning only** unless operators attach external baseline artefacts.")};
    private static (string Headline, string Body) DescribeTenantCaptured(ValueReportSnapshot snapshot, string headline, string headlinePrefix)
    {
        List<string> parts = [$"{headlinePrefix}."];
        if (snapshot.TenantBaselineReviewCycleCapturedUtc is { } cap)
            parts.Add($"**Captured UTC:** `{cap.ToString("O", CultureInfo.InvariantCulture)}`.");
        if (snapshot.TenantBaselineManualPrepHoursPerReview is { } manual)
            parts.Add($"**Manual prep hrs/review:** `{manual.ToString(CultureInfo.InvariantCulture)}`.");
        if (!string.IsNullOrWhiteSpace(snapshot.TenantBaselineReviewCycleSource))
            parts.Add($"**Source note:** {snapshot.TenantBaselineReviewCycleSource.Trim()}");
        string body = string.Join(" ", parts);
        return (headline, body);
    }
}
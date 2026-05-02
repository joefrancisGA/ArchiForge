using System.Globalization;
using System.Text;

namespace ArchLucid.Application.Pilots;

/// <summary>Markdown block for the sponsor-send gate (mirrors structured evaluation; no new numeric claims).</summary>
public static class PilotBuyerSafeEvidenceGateMarkdownFormatter
{
    /// <summary>
    ///     Appends the gate section after introductory copy so PDF export (Markdown-derived) stays aligned.
    /// </summary>
    public static void AppendMarkdownSection(StringBuilder sb, PilotBuyerSafeEvidenceGateResult gate)
    {
        sb.AppendLine("## Sponsor send readiness (buyer-safe gate)");
        sb.AppendLine();
        sb.AppendLine(
            "**Indicator:** Structured checklist from persisted run + tenant ROI baseline window — not a legal or financial attestation.");
        sb.AppendLine();
        sb.AppendLine(
            $"**Publishing posture:** **{DescribeTier(gate.PublishingTier)}** (Complete = no listed gaps and not demo-flagged; Partial = gaps below; Demo-only = seeded tenant).");
        sb.AppendLine();

        if (gate.Gaps.Count is 0)
        {
            sb.AppendLine("**Gaps:** _None detected for the checks above — still review qualitative baselines and attachments._");
            sb.AppendLine();

            return;
        }

        sb.AppendLine("**Gaps (explicit):**");
        sb.AppendLine();

        int n = 1;

        foreach (string gap in gate.Gaps)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{n}. {gap}");
            n++;
        }

        sb.AppendLine();
    }

    private static string DescribeTier(PilotBuyerSafeEvidencePublishingTier tier) => tier switch
    {
        PilotBuyerSafeEvidencePublishingTier.Complete => "Complete",
        PilotBuyerSafeEvidencePublishingTier.Partial => "Partial",
        PilotBuyerSafeEvidencePublishingTier.DemoOnly => "Demo-only",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
    };
}

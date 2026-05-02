using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.ValueReports;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Derives a sponsor-send posture from persisted run + tenant value-window facts — no fabricated completeness.
/// </summary>
public static class PilotBuyerSafeEvidenceGateEvaluator
{
    /// <summary>
    ///     Computes <see cref="PilotBuyerSafeEvidenceGateResult"/> from the same inputs rendered in the first-value
    ///     report. <see cref="PilotBuyerSafeEvidencePublishingTier.DemoOnly"/> wins for publishing when the tenant is
    ///     flagged demo; structural gaps are still listed for internal operators.
    /// </summary>
    public static PilotBuyerSafeEvidenceGateResult Evaluate(
        ArchitectureRun run,
        GoldenManifest? manifest,
        PilotRunDeltas deltas,
        ValueReportSnapshot valueWindowSnapshot)
    {
        if (run is null) throw new ArgumentNullException(nameof(run));
        if (deltas is null) throw new ArgumentNullException(nameof(deltas));
        if (valueWindowSnapshot is null) throw new ArgumentNullException(nameof(valueWindowSnapshot));

        List<string> gaps = [];

        if (deltas.IsDemoTenant)
        {
            gaps.Add("Seeded/demo tenant — replace before external sponsor screenshots or purchase narratives.");
        }

        if (manifest is null || run.Status != ArchitectureRunStatus.Committed)
        {
            gaps.Add(
                "Committed golden manifest absent or run not in Committed status — finalize before external sponsor distribution.");
        }

        if (run.RealModeFellBackToSimulator)
        {
            gaps.Add("Run recorded **simulator substitution** — disclose when claiming real LLM / production agent evidence.");
        }

        if (deltas.TopFindingId is not null && deltas.TopFindingEvidenceChain is null)
        {
            gaps.Add(
                "Top-severity finding present but evidence-chain pointers did not resolve — verify full run detail JSON before sponsor send.");
        }

        if (deltas.AuditRowCount == 0)
        {
            gaps.Add(
                "Scoped audit-event query returned zero rows — confirm tenancy scope and audit continuity for this run.");
        }

        ReviewCycleBaselineProvenance prov = valueWindowSnapshot.ReviewCycleBaselineProvenance;

        if (prov is ReviewCycleBaselineProvenance.NoMeasurementYet or ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions)
        {
            gaps.Add(
                "ROI comparative narrative uses **partial / default** baseline posture (see **ROI evidence completeness** section) — avoid customer-specific dollar claims.");
        }

        PilotBuyerSafeEvidencePublishingTier tier = ResolveTier(deltas.IsDemoTenant, gaps);

        return new PilotBuyerSafeEvidenceGateResult(tier, gaps);
    }

    private static PilotBuyerSafeEvidencePublishingTier ResolveTier(bool demoTenant, IReadOnlyList<string> gaps)
    {
        if (demoTenant)
            return PilotBuyerSafeEvidencePublishingTier.DemoOnly;

        if (gaps.Count is 0)
            return PilotBuyerSafeEvidencePublishingTier.Complete;

        return PilotBuyerSafeEvidencePublishingTier.Partial;
    }
}

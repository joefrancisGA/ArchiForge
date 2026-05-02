namespace ArchLucid.Application.Pilots;

/// <summary>Outcome of the buyer-safe gate used by <see cref="FirstValueReportBuilder"/> (Markdown + PDF sibling).</summary>
public sealed record PilotBuyerSafeEvidenceGateResult(
    PilotBuyerSafeEvidencePublishingTier PublishingTier,
    IReadOnlyList<string> Gaps);

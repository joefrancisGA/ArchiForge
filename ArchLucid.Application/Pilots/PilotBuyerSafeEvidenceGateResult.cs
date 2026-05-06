namespace ArchLucid.Application.Pilots;
/// <summary>Outcome of the buyer-safe gate used by <see cref = "FirstValueReportBuilder"/> (Markdown + PDF sibling).</summary>
public sealed record PilotBuyerSafeEvidenceGateResult(PilotBuyerSafeEvidencePublishingTier PublishingTier, IReadOnlyList<string> Gaps)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Gaps);
    private static byte __ValidatePrimaryConstructorArguments(System.Collections.Generic.IReadOnlyList<System.String> Gaps)
    {
        ArgumentNullException.ThrowIfNull(Gaps);
        return (byte)0;
    }
}
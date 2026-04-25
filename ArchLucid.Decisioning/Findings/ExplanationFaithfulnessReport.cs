namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Result of <see cref="IExplanationFaithfulnessChecker.CheckFaithfulness" /> — a coarse overlap heuristic, not
///     semantic entailment.
/// </summary>
public sealed record ExplanationFaithfulnessReport(
    int ClaimsChecked,
    int ClaimsSupported,
    int ClaimsUnsupported,
    double SupportRatio,
    IReadOnlyList<string> UnsupportedClaims);

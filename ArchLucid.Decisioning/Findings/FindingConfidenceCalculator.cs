using ArchLucid.Contracts.Findings;

namespace ArchLucid.Decisioning.Findings;

/// <summary>
///     Combines schema/harness pass, optional reference-case match, and explainability trace completeness into a single
///     operator-facing score.
/// </summary>
public sealed class FindingConfidenceCalculator
{
    /// <summary>
    ///     Maps additive signals to a 0–100 score and confidence level. Does not throw for finite numeric inputs.
    /// </summary>
    /// <param name="schemaValidationPassed">Quality harness / structural gate acceptance (+35).</param>
    /// <param name="referenceCaseMatched">Reference corpus matched (+40).</param>
    /// <param name="traceCompletenessRatio">Share filled in [0,1]; contributes up to +25.</param>
    public FindingConfidenceCalculationResult? Calculate(
        bool schemaValidationPassed,
        bool referenceCaseMatched,
        decimal? traceCompletenessRatio)
    {
        try
        {
            int score = 0;

            if (schemaValidationPassed)
                score += 35;

            if (referenceCaseMatched)
                score += 40;

            double ratioRaw = traceCompletenessRatio is { } d ? (double)d : 0.0;

            if (double.IsNaN(ratioRaw) || double.IsInfinity(ratioRaw))
                return null;

            double clamped = Math.Clamp(ratioRaw, 0.0, 1.0);
            score += (int)Math.Round(clamped * 25.0, MidpointRounding.AwayFromZero);
            score = Math.Clamp(score, 0, 100);

            FindingConfidenceLevel level = score >= 75
                ? FindingConfidenceLevel.High
                : score >= 45
                    ? FindingConfidenceLevel.Medium
                    : FindingConfidenceLevel.Low;

            return new FindingConfidenceCalculationResult(score, level);
        }
        catch
        {
            return null;
        }
    }
}

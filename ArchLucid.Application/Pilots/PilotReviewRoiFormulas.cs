namespace ArchLucid.Application.Pilots;

/// <summary>
///     ROI math aligned with <c>docs/go-to-market/ROI_MODEL.md</c> §2 status-quo annual review cost and §3.1 50% architect-hour
///     reduction for the review-time lever (conservative).
/// </summary>
public static class PilotReviewRoiFormulas
{
    /// <summary>Annual review labor cost before ArchLucid: reviews/quarter × 4 × hours/review × hourly cost.</summary>
    public static decimal AnnualReviewCostStatusQuo(decimal reviewsPerQuarter, decimal hoursPerReview, decimal hourlyCost) =>
        reviewsPerQuarter * 4m * hoursPerReview * hourlyCost;

    /// <summary>
    ///     Annual review cost after applying the §3.1 conservative 50% reduction in architect hours per review (same reviews
    ///     per year, half the hours).
    /// </summary>
    public static decimal AnnualReviewCostWithArchLucidReviewLever(
        decimal reviewsPerQuarter,
        decimal hoursPerReview,
        decimal hourlyCost) =>
        reviewsPerQuarter * 4m * (hoursPerReview * 0.50m) * hourlyCost;

    /// <summary>Difference between status quo and reduced-hour model (equals half of <see cref="AnnualReviewCostStatusQuo" /> when inputs match).</summary>
    public static decimal AnnualReviewSavings(
        decimal reviewsPerQuarter,
        decimal hoursPerReview,
        decimal hourlyCost) =>
        AnnualReviewCostStatusQuo(reviewsPerQuarter, hoursPerReview, hourlyCost)
        - AnnualReviewCostWithArchLucidReviewLever(reviewsPerQuarter, hoursPerReview, hourlyCost);
}

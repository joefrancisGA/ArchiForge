namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Urgency string values used by <see cref="AlertEvaluator"/> when reading
/// <see cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.Urgency"/>.
/// Matches the values written by <c>RecommendationGenerator</c> so threshold comparisons
/// are case-insensitive duplicates of the same canonical strings.
/// </summary>
public static class AlertUrgencies
{
    public const string Critical = "Critical";
    public const string High = "High";
}

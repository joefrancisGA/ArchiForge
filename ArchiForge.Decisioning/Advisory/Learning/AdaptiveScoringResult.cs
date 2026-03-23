namespace ArchiForge.Decisioning.Advisory.Learning;

/// <summary>
/// Outcome of <see cref="IAdaptiveRecommendationScorer.Score"/> for diagnostics and assigning <see cref="ImprovementRecommendation.PriorityScore"/>.
/// </summary>
public class AdaptiveScoringResult
{
    /// <summary>Copy of <see cref="AdaptiveScoringInput.BasePriorityScore"/>.</summary>
    public int BasePriorityScore
    {
        get; set;
    }

    /// <summary>Weighted, rounded score to persist or display.</summary>
    public int AdaptedPriorityScore
    {
        get; set;
    }

    /// <summary>Multiplier applied for category (defaults to <c>1.0</c>).</summary>
    public double CategoryWeight
    {
        get; set;
    }

    /// <summary>Multiplier applied for urgency (defaults to <c>1.0</c>).</summary>
    public double UrgencyWeight
    {
        get; set;
    }

    /// <summary>Multiplier applied for signal type when matched (defaults to <c>1.0</c>).</summary>
    public double SignalTypeWeight
    {
        get; set;
    }

    /// <summary>Explanation strings (e.g. which weights were applied or that the profile was missing).</summary>
    public List<string> Notes { get; set; } = [];
}

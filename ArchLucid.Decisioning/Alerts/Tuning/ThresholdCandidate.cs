namespace ArchLucid.Decisioning.Alerts.Tuning;

/// <summary>
///     Identifies a single threshold value in a recommendation sweep (paired with simulation in
///     <see cref="ThresholdCandidateEvaluation" />).
/// </summary>
public class ThresholdCandidate
{
    /// <summary>Threshold applied to the tuned metric.</summary>
    public decimal ThresholdValue
    {
        get;
        set;
    }

    /// <summary>Display label (optional; may mirror the numeric value).</summary>
    public string Label
    {
        get;
        set;
    } = null!;
}

namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Decomposed priority signals used to rank plans (deterministic scoring applied by a future service).
/// </summary>
public sealed class ImprovementPlanPriority
{
    /// <summary>Combined rank (higher = more urgent); formula owned by planning/prioritization logic.</summary>
    public int PriorityScore { get; init; }

    /// <summary>Weight from repetition / volume of evidence.</summary>
    public int FrequencyScore { get; init; }

    /// <summary>Weight from severity band or bad-outcome mass.</summary>
    public int SeverityScore { get; init; }

    /// <summary>
    /// Weight from trust posture (e.g. low average trust on related signals). Scale defined by prioritization rules.
    /// </summary>
    public double TrustImpactScore { get; init; }

    /// <summary>Optional deterministic explanation string for operators (filled by future scoring).</summary>
    public string? Explanation { get; init; }
}

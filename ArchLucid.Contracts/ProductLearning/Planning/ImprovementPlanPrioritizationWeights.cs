namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Relative weights for batch prioritization (must sum to 1.0 within tolerance).</summary>
public sealed class ImprovementPlanPrioritizationWeights
{
    /// <summary>Weight for how often the issue appears (signal volume).</summary>
    public double Frequency { get; init; } = 0.40;

    /// <summary>Weight for negative / revision outcome mass.</summary>
    public double Severity { get; init; } = 0.30;

    /// <summary>Weight for low trust (inverted trust score).</summary>
    public double TrustImpact { get; init; } = 0.20;

    /// <summary>Weight for number of distinct artifact facets touched.</summary>
    public double Breadth { get; init; } = 0.10;
}

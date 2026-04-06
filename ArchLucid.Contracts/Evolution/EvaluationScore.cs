namespace ArchiForge.Contracts.Evolution;

/// <summary>Normalized or domain-specific scores for a simulation or aggregate evaluation (scale defined by producer).</summary>
public sealed class EvaluationScore
{
    public double? SimulationScore { get; init; }

    public double? DeterminismScore { get; init; }

    public double? RegressionRiskScore { get; init; }

    /// <summary>Composite improvement vs baseline in [-1,1]: warnings, structure (adds vs removals). Higher is better.</summary>
    public double? ImprovementDelta { get; init; }

    /// <summary>Sorted, human-readable regression/determinism signals (explainability).</summary>
    public IReadOnlyList<string> RegressionSignals { get; init; } = [];

    /// <summary>Trust in inputs and evaluation coverage, [0,1]. Lower when manifests or determinism are missing.</summary>
    public double? ConfidenceScore { get; init; }
}

namespace ArchiForge.Api.Models.Evolution;

/// <summary>API projection of <see cref="ArchiForge.Contracts.Evolution.EvaluationScore"/>.</summary>
public sealed class EvaluationScoreResponse
{
    public double? SimulationScore { get; init; }

    public double? DeterminismScore { get; init; }

    public double? RegressionRiskScore { get; init; }

    public double? ImprovementDelta { get; init; }

    public IReadOnlyList<string> RegressionSignals { get; init; } = [];

    public double? ConfidenceScore { get; init; }
}

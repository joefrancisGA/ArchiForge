namespace ArchiForge.Contracts.Evolution;

/// <summary>How a shadow simulation evaluated baselines (additive modes over time).</summary>
public static class EvolutionEvaluationModeValues
{
    /// <summary>Read-only architecture analysis (no replay commits, no determinism iterations).</summary>
    public const string ReadOnlyArchitectureAnalysis = "ReadOnlyArchitectureAnalysis";
}

namespace ArchLucid.Decisioning.Findings;

/// <summary>
/// Standard <see cref="ArchLucid.Decisioning.Models.ExplainabilityTrace.AlternativePathsConsidered"/> entries for
/// non-LLM engines so completeness analyzers and narratives do not treat the field as permanently empty.
/// </summary>
public static class ExplainabilityTraceMarkers
{
    /// <summary>
    /// Single deterministic outcome path — no multi-branch exploration like stochastic or LLM reasoning.
    /// </summary>
    public const string RuleBasedDeterministicSinglePathNote =
        "Deterministic rule evaluation: exploratory alternatives were not branch-explored (single outcome path).";
}

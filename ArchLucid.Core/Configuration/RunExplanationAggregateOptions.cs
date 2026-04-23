namespace ArchLucid.Core.Configuration;

/// <summary>
///     Aggregate run explanation (<c>GET …/explain/runs/{{runId}}/aggregate</c>) behavior after LLM completion.
/// </summary>
public sealed class RunExplanationAggregateOptions
{
    public const string SectionPath = "ArchLucid:Explanation:Aggregate";

    /// <summary>When true, replace the LLM narrative with deterministic manifest text when faithfulness is very low.</summary>
    public bool FaithfulnessFallbackEnabled
    {
        get;
        set;
    } = true;

    /// <summary>
    ///     When <see cref="ExplanationFaithfulnessChecker" /> reports at least one claim checked and
    ///     <see cref="ArchLucid.Decisioning.Findings.ExplanationFaithfulnessReport.SupportRatio" /> is strictly below this
    ///     value,
    ///     the aggregate uses the deterministic explanation builder with an empty LLM payload (manifest-derived text only).
    /// </summary>
    public double MinSupportRatioToTrustLlmNarrative
    {
        get;
        set;
    } = 0.2;
}

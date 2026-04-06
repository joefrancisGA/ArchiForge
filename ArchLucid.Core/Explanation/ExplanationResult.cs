namespace ArchiForge.Core.Explanation;

/// <summary>
/// Stakeholder-facing explanation for a single run: short summary, bullet facets, and longer narrative.
/// </summary>
/// <remarks>Produced by <c>ArchiForge.AgentRuntime.Explanation.IExplanationService.ExplainRunAsync</c>.</remarks>
public class ExplanationResult
{
    /// <summary>One-paragraph headline (LLM or manifest metadata fallback).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Decision/topology/compliance drivers extracted for the prompt.</summary>
    public List<string> KeyDrivers { get; set; } = [];

    /// <summary>Open issues and warnings summarized as bullets.</summary>
    public List<string> RiskImplications { get; set; } = [];

    /// <summary>Cost ceiling and cost-risk bullets.</summary>
    public List<string> CostImplications { get; set; } = [];

    /// <summary>Compliance control counts and gap lines.</summary>
    public List<string> ComplianceImplications { get; set; } = [];

    /// <summary>Multi-paragraph narrative (LLM or deterministic fallback).</summary>
    public string DetailedNarrative { get; set; } = string.Empty;
}

/// <summary>
/// Narrative bundle for a manifest-to-manifest comparison (base → target).
/// </summary>
/// <remarks>Produced by <c>ArchiForge.AgentRuntime.Explanation.IExplanationService.ExplainComparisonAsync</c>.</remarks>
public class ComparisonExplanationResult
{
    /// <summary>Short executive summary of what changed.</summary>
    public string HighLevelSummary { get; set; } = string.Empty;

    /// <summary>Structured major change lines (decisions + capped requirement deltas).</summary>
    public List<string> MajorChanges { get; set; } = [];

    /// <summary>Tradeoff bullets from the model when available.</summary>
    public List<string> KeyTradeoffs { get; set; } = [];

    /// <summary>Longer comparison story (LLM or fallback stitched from highlights).</summary>
    public string Narrative { get; set; } = string.Empty;
}

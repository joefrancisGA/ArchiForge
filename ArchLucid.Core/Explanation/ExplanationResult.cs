namespace ArchLucid.Core.Explanation;

/// <summary>
///     Stakeholder-facing explanation for a single run: short summary, bullet facets, longer narrative, and optional
///     structured LLM envelope.
/// </summary>
/// <remarks>Produced by <c>ArchLucid.AgentRuntime.Explanation.IExplanationService.ExplainRunAsync</c>.</remarks>
public class ExplanationResult
{
    /// <summary>Raw LLM completion (after JSON fence unwrap), for auditing and backward-compatible opaque access.</summary>
    public string RawText
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Programmatic explanation envelope when the model returned structured JSON; always set (fallback wraps free
    ///     text).
    /// </summary>
    public StructuredExplanation? Structured
    {
        get;
        set;
    }

    /// <summary>
    ///     Convenience mirror of <see cref="StructuredExplanation.Confidence" /> for operator surfaces without opening
    ///     the structured envelope.
    /// </summary>
    public decimal? Confidence
    {
        get;
        set;
    }

    /// <summary>Which agent role, model/deployment, and prompt revision produced this explanation.</summary>
    public ExplanationProvenance? Provenance
    {
        get;
        set;
    }

    /// <summary>One-paragraph headline (LLM or manifest metadata fallback).</summary>
    public string Summary
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Decision/topology/compliance drivers extracted for the prompt.</summary>
    public List<string> KeyDrivers
    {
        get;
        set;
    } = [];

    /// <summary>Open issues and warnings summarized as bullets.</summary>
    public List<string> RiskImplications
    {
        get;
        set;
    } = [];

    /// <summary>Cost ceiling and cost-risk bullets.</summary>
    public List<string> CostImplications
    {
        get;
        set;
    } = [];

    /// <summary>Compliance control counts and gap lines.</summary>
    public List<string> ComplianceImplications
    {
        get;
        set;
    } = [];

    /// <summary>Multi-paragraph narrative (LLM or deterministic fallback).</summary>
    public string DetailedNarrative
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     Per-finding explainability trace completeness when the API attaches snapshot context (see granular explain
    ///     endpoint).
    /// </summary>
    public List<FindingTraceConfidenceDto>? FindingTraceConfidences
    {
        get;
        set;
    }
}

/// <summary>
///     Narrative bundle for a manifest-to-manifest comparison (base → target).
/// </summary>
/// <remarks>Produced by <c>ArchLucid.AgentRuntime.Explanation.IExplanationService.ExplainComparisonAsync</c>.</remarks>
public class ComparisonExplanationResult
{
    /// <summary>Short executive summary of what changed.</summary>
    public string HighLevelSummary
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Structured major change lines (decisions + capped requirement deltas).</summary>
    public List<string> MajorChanges
    {
        get;
        set;
    } = [];

    /// <summary>Tradeoff bullets from the model when available.</summary>
    public List<string> KeyTradeoffs
    {
        get;
        set;
    } = [];

    /// <summary>Longer comparison story (LLM or fallback stitched from highlights).</summary>
    public string Narrative
    {
        get;
        set;
    } = string.Empty;
}

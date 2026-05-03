using ArchLucid.Contracts.Findings;

namespace ArchLucid.Contracts.Explanation;

/// <summary>
///     Deterministic explainability payload for a single finding (from persisted <c>ExplainabilityTrace</c>, no LLM).
/// </summary>
public sealed class FindingExplainabilityResult
{
    public string FindingId
    {
        get;
        set;
    } = string.Empty;

    public string Title
    {
        get;
        set;
    } = string.Empty;

    public string EngineType
    {
        get;
        set;
    } = string.Empty;

    public string Severity
    {
        get;
        set;
    } = string.Empty;

    public double TraceCompletenessRatio
    {
        get;
        set;
    }

    /// <summary>Trace dimensions that were empty when completeness was scored.</summary>
    public List<string> MissingTraceFields
    {
        get;
        set;
    } = [];

    public List<string> GraphNodeIdsExamined
    {
        get;
        set;
    } = [];

    public List<string> RulesApplied
    {
        get;
        set;
    } = [];

    public List<string> DecisionsTaken
    {
        get;
        set;
    } = [];

    public List<string> AlternativePathsConsidered
    {
        get;
        set;
    } = [];

    public List<string> Notes
    {
        get;
        set;
    } = [];

    /// <summary>
    ///     Structured factual explainability (trace + finding rationale); always populated by the API from persisted data.
    /// </summary>
    public FindingExplainabilityEvidence Evidence
    {
        get;
        set;
    } =
        new(
            [],
            string.Empty,
            [],
            "unspecified");

    /// <summary>Deterministic plain-text narrative composed from explainability trace fields (presentation only).</summary>
    public string NarrativeText
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Evaluation-derived confidence in [0,100] when persisted with evaluation wiring.</summary>
    public int? EvaluationConfidenceScore
    {
        get;
        set;
    }

    /// <summary>Mapped bucket for <see cref="EvaluationConfidenceScore" />.</summary>
    public FindingConfidenceLevel? ConfidenceLevel
    {
        get;
        set;
    }
}

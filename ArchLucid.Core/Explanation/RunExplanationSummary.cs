using ArchLucid.Contracts.Explanation;

namespace ArchLucid.Core.Explanation;

/// <summary>
///     Executive rollup for a single run: the same <see cref="ExplanationResult" /> as granular explain, plus themes,
///     posture, and counts.
/// </summary>
public sealed class RunExplanationSummary
{
    public required ExplanationResult Explanation
    {
        get;
        init;
    }

    public required List<string> ThemeSummaries
    {
        get;
        init;
    }

    public required string OverallAssessment
    {
        get;
        init;
    }

    public required string RiskPosture
    {
        get;
        init;
    }

    public int FindingCount
    {
        get;
        init;
    }

    public int DecisionCount
    {
        get;
        init;
    }

    public int UnresolvedIssueCount
    {
        get;
        init;
    }

    public int ComplianceGapCount
    {
        get;
        init;
    }

    /// <summary>Faithfulness support ratio from the findings faithfulness checker when findings exist; otherwise null.</summary>
    public double? FaithfulnessSupportRatio
    {
        get;
        init;
    }

    /// <summary>True when low faithfulness triggered replacement of the LLM narrative with deterministic manifest text.</summary>
    public bool UsedDeterministicFallback
    {
        get;
        init;
    }

    /// <summary>Operator-facing note when faithfulness is weak; null when not applicable.</summary>
    public string? FaithfulnessWarning
    {
        get;
        init;
    }

    /// <summary>Per-finding trace completeness (same order as snapshot findings when populated).</summary>
    public IReadOnlyList<FindingTraceConfidenceDto>? FindingTraceConfidences
    {
        get;
        init;
    }

    /// <summary>Persisted artifacts backing narrative text (manifest, traces, findings, optional bundle).</summary>
    public IReadOnlyList<CitationReference> Citations
    {
        get;
        init;
    }
        = [];
}

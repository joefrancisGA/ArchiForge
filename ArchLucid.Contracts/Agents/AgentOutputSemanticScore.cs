using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Agents;

/// <summary>Semantic quality score for a single agent trace output (claims evidence + finding completeness).</summary>
public sealed class AgentOutputSemanticScore
{
    public string TraceId { get; set; } = string.Empty;

    public AgentType AgentType { get; set; }

    /// <summary>Fraction of claims that have non-empty evidence references or evidence string.</summary>
    public double ClaimsQualityRatio { get; set; }

    /// <summary>Fraction of findings with non-empty severity, description (&gt;10 chars), and recommendation (&gt;5 chars).</summary>
    public double FindingsQualityRatio { get; set; }

    public int EmptyClaimCount { get; set; }

    public int IncompleteFindingCount { get; set; }

    /// <summary>Weighted average: Claims * 0.4 + Findings * 0.6. Zero when both denominators are zero.</summary>
    public double OverallSemanticScore { get; set; }
}

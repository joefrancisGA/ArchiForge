using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>Semantic quality score for a single agent trace output (claims evidence + finding completeness).</summary>
public sealed class AgentOutputSemanticScore
{
    public required string TraceId { get; init; }

    public required AgentType AgentType { get; init; }

    /// <summary>Fraction of claims that have non-empty evidence references.</summary>
    public double ClaimsQualityRatio { get; init; }

    /// <summary>Fraction of findings with non-empty severity, description (>10 chars), and recommendation (>5 chars).</summary>
    public double FindingsQualityRatio { get; init; }

    public int EmptyClaimCount { get; init; }

    public int IncompleteFindingCount { get; init; }

    /// <summary>Weighted average: Claims * 0.4 + Findings * 0.6. Zero when both denominators are zero.</summary>
    public double OverallSemanticScore { get; init; }
}

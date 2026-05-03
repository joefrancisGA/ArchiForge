namespace ArchLucid.Contracts.Findings;

/// <summary>
///     Operator-facing coarse bucket derived from gate + reference-case + trace completeness scoring.
/// </summary>
public enum FindingConfidenceLevel
{
    High = 0,

    Medium = 1,

    Low = 2,
}

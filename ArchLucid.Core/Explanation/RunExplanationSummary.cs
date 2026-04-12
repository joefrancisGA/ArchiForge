namespace ArchLucid.Core.Explanation;

/// <summary>
/// Executive rollup for a single run: the same <see cref="ExplanationResult"/> as granular explain, plus themes, posture, and counts.
/// </summary>
public sealed class RunExplanationSummary
{
    public required ExplanationResult Explanation { get; init; }

    public required List<string> ThemeSummaries { get; init; }

    public required string OverallAssessment { get; init; }

    public required string RiskPosture { get; init; }

    public int FindingCount { get; init; }

    public int DecisionCount { get; init; }

    public int UnresolvedIssueCount { get; init; }

    public int ComplianceGapCount { get; init; }
}

namespace ArchLucid.Decisioning.Findings;

/// <summary>Aggregated trace completeness for one <see cref="Models.Finding.EngineType"/>.</summary>
public sealed class EngineTraceCompleteness
{
    public string EngineType { get; init; } = null!;

    public int FindingCount { get; init; }

    public double CompletenessRatio { get; init; }

    public int GraphNodeIdsPopulatedCount { get; init; }

    public int RulesAppliedPopulatedCount { get; init; }

    public int DecisionsTakenPopulatedCount { get; init; }

    public int AlternativePathsPopulatedCount { get; init; }

    public int NotesPopulatedCount { get; init; }
}

namespace ArchLucid.Decisioning.Findings;

/// <summary>Snapshot-level aggregation of explainability trace completeness.</summary>
public sealed class TraceCompletenessSummary
{
    public int TotalFindings { get; init; }

    public double OverallCompletenessRatio { get; init; }

    public List<EngineTraceCompleteness> ByEngine { get; init; } = [];
}

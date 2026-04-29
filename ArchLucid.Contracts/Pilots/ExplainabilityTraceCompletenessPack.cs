namespace ArchLucid.Contracts.Pilots;

/// <summary>Explainability-trace completeness for findings in the persisted <c>FindingsSnapshot</c> for the demo run.</summary>
public sealed class ExplainabilityTraceCompletenessPack
{
    public int TotalFindings
    {
        get;
        init;
    }

    /// <summary>Average completeness ratio across findings (each finding scores 0..1 from populated trace slices).</summary>
    public double OverallCompletenessRatio
    {
        get;
        init;
    }

    public IReadOnlyList<ExplainabilityTraceEngineCompletenessPack> ByEngine
    {
        get;
        init;
    } = [];
}

/// <summary>Per-engine aggregate for <see cref="ExplainabilityTraceCompletenessPack" />.</summary>
public sealed class ExplainabilityTraceEngineCompletenessPack
{
    public string EngineType
    {
        get;
        init;
    } = string.Empty;

    public int FindingCount
    {
        get;
        init;
    }

    public double CompletenessRatio
    {
        get;
        init;
    }

    public int GraphNodeIdsPopulatedCount
    {
        get;
        init;
    }

    public int RulesAppliedPopulatedCount
    {
        get;
        init;
    }

    public int DecisionsTakenPopulatedCount
    {
        get;
        init;
    }

    public int AlternativePathsPopulatedCount
    {
        get;
        init;
    }

    public int NotesPopulatedCount
    {
        get;
        init;
    }
}

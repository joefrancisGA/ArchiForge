namespace ArchLucid.Core.Explanation;

/// <summary>
///     API projection of explainability trace coverage for a single finding (mirrors <c>TraceCompletenessScore</c> shape).
/// </summary>
public sealed class FindingTraceCompletenessScore
{
    public string FindingId
    {
        get;
        set;
    } = string.Empty;

    public string EngineType
    {
        get;
        set;
    } = string.Empty;

    public bool HasGraphNodeIds
    {
        get;
        set;
    }

    public bool HasRulesApplied
    {
        get;
        set;
    }

    public bool HasDecisionsTaken
    {
        get;
        set;
    }

    public bool HasAlternativePaths
    {
        get;
        set;
    }

    public bool HasNotes
    {
        get;
        set;
    }

    public int PopulatedFieldCount
    {
        get;
        set;
    }

    public double CompletenessRatio
    {
        get;
        set;
    }
}

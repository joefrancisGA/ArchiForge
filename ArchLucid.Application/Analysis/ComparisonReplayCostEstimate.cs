namespace ArchLucid.Application.Analysis;

/// <summary>
///     Relative guidance derived from comparison type, replay mode, output format, and payload size — not wall-clock SLA.
/// </summary>
public sealed class ComparisonReplayCostEstimate
{
    public required string ComparisonRecordId
    {
        get;
        init;
    }

    public required string ComparisonType
    {
        get;
        init;
    }

    public required string Format
    {
        get;
        init;
    }

    public required string ReplayMode
    {
        get;
        init;
    }

    public bool PersistReplay
    {
        get;
        init;
    }

    /// <summary>Opaque 0–100-ish score; higher means more CPU/IO expected.</summary>
    public int ApproximateRelativeScore
    {
        get;
        init;
    }

    /// <summary>One of <c>low</c>, <c>medium</c>, <c>high</c>.</summary>
    public required string RelativeCostBand
    {
        get;
        init;
    }

    public required IReadOnlyList<string> Factors
    {
        get;
        init;
    }
}

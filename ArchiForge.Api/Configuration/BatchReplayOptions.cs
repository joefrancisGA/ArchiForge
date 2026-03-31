namespace ArchiForge.Api.Configuration;

/// <summary>Limits and behavior for <c>POST .../comparisons/replay/batch</c>.</summary>
public sealed class BatchReplayOptions
{
    public const string SectionName = "ComparisonReplay:Batch";

    /// <summary>Maximum number of comparison record IDs accepted in one batch (distinct IDs are processed).</summary>
    public int MaxComparisonRecordIds { get; set; } = 50;
}

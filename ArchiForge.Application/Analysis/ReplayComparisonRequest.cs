namespace ArchiForge.Application.Analysis;

public sealed class ReplayComparisonRequest
{
    public string ComparisonRecordId { get; set; } = string.Empty;

    public string Format { get; set; } = "markdown";

    /// <summary>Export profile for end-to-end comparison: default, short, detailed, executive.</summary>
    public string? Profile { get; set; }
}


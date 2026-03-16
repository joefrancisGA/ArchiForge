namespace ArchiForge.Application.Analysis;

public sealed class ReplayComparisonRequest
{
    public string ComparisonRecordId { get; set; } = string.Empty;

    public string Format { get; set; } = "markdown";
}


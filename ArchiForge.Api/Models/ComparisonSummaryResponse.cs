namespace ArchiForge.Api.Models;

public sealed class ComparisonSummaryResponse
{
    public string ComparisonRecordId { get; set; } = string.Empty;

    public string ComparisonType { get; set; } = string.Empty;

    public string Format { get; set; } = "markdown";

    public string Summary { get; set; } = string.Empty;
}


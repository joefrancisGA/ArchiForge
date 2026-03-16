namespace ArchiForge.Api.Models;

public sealed class ExportRecordDiffSummaryResponse
{
    public string Format { get; set; } = "markdown";

    public string Summary { get; set; } = string.Empty;
}


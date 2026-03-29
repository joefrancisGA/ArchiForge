namespace ArchiForge.Api.Models;

/// <summary>
/// Payload for synchronous markdown export of a run’s analysis report (<c>POST .../analysis-report/export</c>).
/// </summary>
public sealed class ArchitectureAnalysisExportResponse
{
    /// <summary>Run whose report was generated.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Report serialization format (currently <c>markdown</c>).</summary>
    public string Format { get; set; } = "markdown";

    /// <summary>Suggested download filename for the report body.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Full markdown document body.</summary>
    public string Content { get; set; } = string.Empty;
}

using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class BatchReplayComparisonRequest
{
    public List<string> ComparisonRecordIds { get; set; } = [];
    public string Format { get; set; } = "markdown";
    public string ReplayMode { get; set; } = "artifact";
    public string? Profile { get; set; }
    public bool PersistReplay { get; set; }
}


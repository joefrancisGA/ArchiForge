namespace ArchiForge.Api.Models;

public sealed class BatchReplayComparisonRequest
{
    public List<string> ComparisonRecordIds { get; set; } = [];

    public string Format { get; set; } = "markdown";

    public string ReplayMode { get; set; } = "artifact";

    public string? Profile { get; set; }

    public bool PersistReplay { get; set; }
}


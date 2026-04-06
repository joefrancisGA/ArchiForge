namespace ArchiForge.Contracts.Architecture;

/// <summary>
/// Lightweight run summary for list endpoints and dashboard views.
/// Sourced from <c>IRunDetailQueryService.ListRunSummariesAsync</c>.
/// </summary>
public sealed class RunSummary
{
    public string RunId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Status string (e.g. "Committed", "ReadyForCommit") matching <c>ArchitectureRunStatus</c> names.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? CurrentManifestVersion { get; set; }
    public string SystemName { get; set; } = string.Empty;
}

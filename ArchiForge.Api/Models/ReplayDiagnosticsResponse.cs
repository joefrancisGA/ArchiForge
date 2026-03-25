namespace ArchiForge.Api.Models;

/// <summary>Diagnostics payload listing recent replay executions for the diagnostics endpoint.</summary>
public sealed class ReplayDiagnosticsResponse
{
    public List<ReplayDiagnosticsEntryDto> RecentReplays { get; set; } = [];
}

using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>Diagnostics payload listing recent replay executions for the diagnostics endpoint.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayDiagnosticsResponse
{
    public List<ReplayDiagnosticsEntryDto> RecentReplays { get; set; } = [];
}

using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>Shared query model for run-vs-run comparison endpoints.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class RunPairQuery
{
    public string LeftRunId { get; set; } = string.Empty;
    public string RightRunId { get; set; } = string.Empty;
}

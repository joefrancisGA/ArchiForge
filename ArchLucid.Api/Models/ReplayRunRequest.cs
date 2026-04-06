using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayRunRequest
{
    public bool CommitReplay { get; set; } = false;
    public string ExecutionMode { get; set; } = "Current";
    public string? ManifestVersionOverride { get; set; }
}

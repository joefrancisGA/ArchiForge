using System.Diagnostics.CodeAnalysis;

using ArchiForge.Application.Diffs;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class AgentResultCompareSummaryResponse
{
    public string Format { get; set; } = "markdown";
    public string Summary { get; set; } = string.Empty;
    public AgentResultDiffResult Diff { get; set; } = new();
}

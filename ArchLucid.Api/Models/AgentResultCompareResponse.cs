using System.Diagnostics.CodeAnalysis;

using ArchiForge.Application.Diffs;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class AgentResultCompareResponse
{
    public AgentResultDiffResult Diff { get; set; } = new();
}

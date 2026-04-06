using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class SubmitAgentResultResponse
{
    public string ResultId { get; set; } = string.Empty;
}

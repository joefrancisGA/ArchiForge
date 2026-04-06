using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Agents;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class SubmitAgentResultRequest
{
    public AgentResult Result { get; set; } = new();
}

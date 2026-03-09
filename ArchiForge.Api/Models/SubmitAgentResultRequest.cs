using ArchiForge.Contracts.Agents;

namespace ArchiForge.Api.Models;

public sealed class SubmitAgentResultRequest
{
    public AgentResult Result { get; set; } = new();
}
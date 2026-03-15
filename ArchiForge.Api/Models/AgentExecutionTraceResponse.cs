using ArchiForge.Contracts.Agents;

namespace ArchiForge.Api.Models;

public sealed class AgentExecutionTraceResponse
{
    public List<AgentExecutionTrace> Traces { get; set; } = [];
}

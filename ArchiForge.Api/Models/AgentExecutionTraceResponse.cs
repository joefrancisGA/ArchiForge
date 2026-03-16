using ArchiForge.Contracts.Agents;

namespace ArchiForge.Api.Models;

public sealed class AgentExecutionTraceResponse
{
    public List<AgentExecutionTrace> Traces { get; set; } = [];

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }
}

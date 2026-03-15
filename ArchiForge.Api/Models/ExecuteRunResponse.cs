using ArchiForge.Contracts.Agents;

namespace ArchiForge.Api.Models;

public sealed class ExecuteRunResponse
{
    public string RunId { get; set; } = string.Empty;

    public List<AgentResult> Results { get; set; } = [];
}

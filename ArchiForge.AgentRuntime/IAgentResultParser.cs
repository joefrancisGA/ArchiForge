using ArchiForge.Contracts.Agents;

namespace ArchiForge.AgentRuntime;

public interface IAgentResultParser
{
    AgentResult ParseAndValidate(
        string json,
        string runId,
        string taskId);
}

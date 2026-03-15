using ArchiForge.Contracts.Agents;

namespace ArchiForge.Application.Diffs;

public interface IAgentResultDiffService
{
    AgentResultDiffResult Compare(
        string leftRunId,
        IReadOnlyCollection<AgentResult> leftResults,
        string rightRunId,
        IReadOnlyCollection<AgentResult> rightResults);
}

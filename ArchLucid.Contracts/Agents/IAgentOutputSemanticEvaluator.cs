using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Agents;

/// <summary>Evaluates semantic quality of agent output JSON (claim evidence + finding completeness); no LLM.</summary>
public interface IAgentOutputSemanticEvaluator
{
    AgentOutputSemanticScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType);
}

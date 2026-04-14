using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>Evaluates semantic quality of agent output JSON (claim evidence + finding completeness); no LLM.</summary>
public interface IAgentOutputSemanticEvaluator
{
    AgentOutputSemanticScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType);
}

using ArchLucid.Contracts.Common;

namespace ArchLucid.Contracts.Agents;

/// <summary>
///     Pure structural checks on persisted <see cref="AgentExecutionTrace" /> JSON (see
///     <see cref="AgentExecutionTrace.ParsedResultJson" />); no LLM.
/// </summary>
public interface IAgentOutputEvaluator
{
    /// <summary>
    ///     Scores presence of expected top-level JSON properties for <paramref name="agentType" /> (camelCase keys as stored
    ///     in traces).
    /// </summary>
    AgentOutputEvaluationScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType);
}

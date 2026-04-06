using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Deserializes LLM JSON into <see cref="AgentResult"/> and enforces run, task, and agent-type consistency.
/// </summary>
public interface IAgentResultParser
{
    /// <summary>
    /// Parses <paramref name="json"/> and throws <see cref="InvalidOperationException"/> when empty, invalid JSON, or identifiers do not match expectations.
    /// </summary>
    /// <param name="json">Raw assistant output (JSON object).</param>
    /// <param name="expectedRunId">Run id that must match the deserialized result.</param>
    /// <param name="expectedTaskId">Task id that must match the deserialized result.</param>
    /// <param name="expectedAgentType">Agent type that must match the deserialized result.</param>
    /// <returns>The validated <see cref="AgentResult"/>.</returns>
    AgentResult ParseAndValidate(
        string json,
        string expectedRunId,
        string expectedTaskId,
        AgentType expectedAgentType);
}

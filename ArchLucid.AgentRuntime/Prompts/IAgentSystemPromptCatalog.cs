using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Prompts;

/// <summary>Resolves versioned system prompt templates for LLM-backed agents.</summary>
public interface IAgentSystemPromptCatalog
{
    /// <summary>Returns the canonical system prompt and metadata for <paramref name="agentType"/>.</summary>
    /// <exception cref="InvalidOperationException">When <paramref name="agentType"/> has no registered template (e.g. Cost).</exception>
    ResolvedSystemPrompt Resolve(AgentType agentType);
}

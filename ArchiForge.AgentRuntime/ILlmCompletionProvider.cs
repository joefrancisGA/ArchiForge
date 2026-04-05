namespace ArchiForge.AgentRuntime;

/// <summary>
/// LLM vendor abstraction for chat completions used by agents, explanations, and Ask flows.
/// Resolves to the same completion pipeline as <see cref="IAgentCompletionClient"/> with stable provider metadata for metrics and future multi-vendor routing.
/// </summary>
public interface ILlmCompletionProvider : IAgentCompletionClient
{
    /// <summary>Logical provider id (e.g. <c>azure-openai</c>, <c>fake</c>).</summary>
    string ProviderId { get; }

    /// <summary>Deployment or model label (e.g. Azure OpenAI deployment name).</summary>
    string ModelDeploymentLabel { get; }
}

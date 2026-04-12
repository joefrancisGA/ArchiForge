namespace ArchLucid.AgentRuntime;

/// <summary>
/// Alias of <see cref="ILlmProvider"/> for JSON-shaped chat completions used by agents, explanations, and <c>AskService</c>.
/// </summary>
/// <remarks>
/// Implementations should return a single assistant message body suitable for JSON parsing by callers (e.g. <see cref="ArchLucid.AgentRuntime.Explanation.ExplanationService"/>, <c>ArchLucid.Api.Services.Ask.AskService</c>).
/// Production: <see cref="AzureOpenAiCompletionClient"/> (optionally wrapped by <see cref="CachingAgentCompletionClient"/> and <see cref="CircuitBreakingAgentCompletionClient"/>, with <see cref="FallbackAgentCompletionClient"/> outermost when fallback LLM is enabled); tests/dev: <see cref="FakeAgentCompletionClient"/>.
/// For vendor metadata and metrics, use <see cref="ILlmCompletionProvider"/> or <see cref="ILlmProvider.Descriptor"/>.
/// </remarks>
public interface IAgentCompletionClient : ILlmProvider;

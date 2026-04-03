namespace ArchiForge.AgentRuntime;

/// <summary>
/// Thin abstraction over a chat completion model used by agents, explanations, and <c>AskService</c>.
/// </summary>
/// <remarks>
/// Implementations should return a single assistant message body suitable for JSON parsing by callers (e.g. <see cref="ArchiForge.AgentRuntime.Explanation.ExplanationService"/>, <c>ArchiForge.Api.Services.Ask.AskService</c>).
/// Production: <see cref="AzureOpenAiCompletionClient"/> (optionally wrapped by <see cref="CachingAgentCompletionClient"/> and <see cref="CircuitBreakingAgentCompletionClient"/>); tests/dev: <see cref="FakeAgentCompletionClient"/>.
/// </remarks>
public interface IAgentCompletionClient
{
    /// <summary>
    /// Sends <paramref name="systemPrompt"/> and <paramref name="userPrompt"/> as chat messages and returns the assistant text.
    /// </summary>
    /// <param name="systemPrompt">Model behavior and output constraints.</param>
    /// <param name="userPrompt">Task-specific payload (often includes embedded JSON context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw model output string (may include markdown fences; callers often unwrap).</returns>
    Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

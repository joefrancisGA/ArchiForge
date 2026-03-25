namespace ArchiForge.AgentRuntime;

/// <summary>
/// Deterministic <see cref="IAgentCompletionClient"/> for tests and local dev; delegates to a supplied prompt resolver.
/// </summary>
public sealed class FakeAgentCompletionClient(Func<string, string, string> resolver) : IAgentCompletionClient
{
    /// <inheritdoc />
    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        string json = resolver(systemPrompt, userPrompt);
        return Task.FromResult(json);
    }
}

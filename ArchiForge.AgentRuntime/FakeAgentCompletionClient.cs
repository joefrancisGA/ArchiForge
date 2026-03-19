namespace ArchiForge.AgentRuntime;

public sealed class FakeAgentCompletionClient(Func<string, string, string> resolver) : IAgentCompletionClient
{
    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var json = resolver(systemPrompt, userPrompt);
        return Task.FromResult(json);
    }
}

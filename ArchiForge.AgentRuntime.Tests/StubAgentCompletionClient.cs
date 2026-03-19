namespace ArchiForge.AgentRuntime.Tests;

public sealed class StubAgentCompletionClient(string json) : IAgentCompletionClient
{
    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(json);
    }
}

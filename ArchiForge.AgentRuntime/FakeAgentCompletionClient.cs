namespace ArchiForge.AgentRuntime;

public sealed class FakeAgentCompletionClient : IAgentCompletionClient
{
    private readonly Func<string, string, string?, string?, string> _resolver;

    public FakeAgentCompletionClient(Func<string, string, string?, string?, string> resolver)
    {
        _resolver = resolver;
    }

    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        string? runId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var json = _resolver(systemPrompt, userPrompt, runId, taskId);
        return Task.FromResult(json);
    }
}

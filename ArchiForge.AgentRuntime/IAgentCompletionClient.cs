namespace ArchiForge.AgentRuntime;

public interface IAgentCompletionClient
{
    Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        string? runId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);
}

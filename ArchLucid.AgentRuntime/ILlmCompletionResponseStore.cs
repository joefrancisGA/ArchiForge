namespace ArchiForge.AgentRuntime;

/// <summary>Pluggable backing store for <see cref="CachingAgentCompletionClient"/> (memory or distributed).</summary>
public interface ILlmCompletionResponseStore
{
    Task<string?> TryGetAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, string value, TimeSpan absoluteExpiration, CancellationToken cancellationToken);
}

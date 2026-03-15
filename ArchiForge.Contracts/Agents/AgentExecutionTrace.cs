using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Agents;

public sealed class AgentExecutionTrace
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public string RunId { get; set; } = string.Empty;

    public string TaskId { get; set; } = string.Empty;

    public AgentType AgentType { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public string UserPrompt { get; set; } = string.Empty;

    public string RawResponse { get; set; } = string.Empty;

    public string? ParsedResultJson { get; set; }

    public bool ParseSucceeded { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

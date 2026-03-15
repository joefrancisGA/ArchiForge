using ArchiForge.Contracts.Common;

namespace ArchiForge.AgentRuntime;

public interface IAgentExecutionTraceRecorder
{
    Task RecordAsync(
        string runId,
        string taskId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        string? parsedResultJson,
        bool parseSucceeded,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}

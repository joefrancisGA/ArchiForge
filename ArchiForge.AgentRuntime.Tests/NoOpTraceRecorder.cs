using ArchiForge.Contracts.Common;

namespace ArchiForge.AgentRuntime.Tests;

public sealed class NoOpTraceRecorder : IAgentExecutionTraceRecorder
{
    public Task RecordAsync(
        string runId,
        string taskId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        string? parsedResultJson,
        bool parseSucceeded,
        string? errorMessage,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

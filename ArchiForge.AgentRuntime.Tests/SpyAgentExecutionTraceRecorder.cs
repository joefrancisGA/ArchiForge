using ArchiForge.Contracts.Common;

namespace ArchiForge.AgentRuntime.Tests;

/// <summary>Test double that records <see cref="IAgentExecutionTraceRecorder"/>.<c>RecordAsync</c> invocations.</summary>
public sealed class SpyAgentExecutionTraceRecorder : IAgentExecutionTraceRecorder
{
    public List<(string RunId, string TaskId, AgentType AgentType)> Calls { get; } = [];

    /// <inheritdoc />
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
        CancellationToken cancellationToken = default)
    {
        Calls.Add((runId, taskId, agentType));

        return Task.CompletedTask;
    }
}

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>Test double that records <see cref="IAgentExecutionTraceRecorder"/>.<c>RecordAsync</c> invocations.</summary>
public sealed class SpyAgentExecutionTraceRecorder : IAgentExecutionTraceRecorder
{
    public List<(string RunId, string TaskId, AgentType AgentType, string? ModelDeploymentName, string? ModelVersion)>
        Calls { get; } = [];

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
        AgentPromptReproMetadata? promptRepro = null,
        int? inputTokenCount = null,
        int? outputTokenCount = null,
        string? modelDeploymentName = null,
        string? modelVersion = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((runId, taskId, agentType, modelDeploymentName, modelVersion));

        return Task.CompletedTask;
    }
}

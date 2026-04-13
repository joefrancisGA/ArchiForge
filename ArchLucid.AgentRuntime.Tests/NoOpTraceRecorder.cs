using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

namespace ArchLucid.AgentRuntime.Tests;

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
        AgentPromptReproMetadata? promptRepro = null,
        int? inputTokenCount = null,
        int? outputTokenCount = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

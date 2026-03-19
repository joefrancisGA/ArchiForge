using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Repositories;

namespace ArchiForge.AgentRuntime;

public sealed class AgentExecutionTraceRecorder(IAgentExecutionTraceRepository repository)
    : IAgentExecutionTraceRecorder
{
    public async Task RecordAsync(
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
        var trace = new AgentExecutionTrace
        {
            TraceId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            TaskId = taskId,
            AgentType = agentType,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            RawResponse = rawResponse,
            ParsedResultJson = parsedResultJson,
            ParseSucceeded = parseSucceeded,
            ErrorMessage = errorMessage,
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(trace, cancellationToken);
    }
}

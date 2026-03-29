using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Data.Repositories;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// <see cref="IAgentExecutionTraceRecorder"/> that inserts rows via <see cref="IAgentExecutionTraceRepository"/>, truncating large prompt/response fields.
/// </summary>
public sealed class AgentExecutionTraceRecorder(IAgentExecutionTraceRepository repository)
    : IAgentExecutionTraceRecorder
{
    /// <summary>Maximum stored length for prompt/response fields to prevent unbounded PII retention.</summary>
    private const int MaxContentLength = 8192;

    /// <inheritdoc />
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
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        AgentExecutionTrace trace = new()
        {
            TraceId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            TaskId = taskId,
            AgentType = agentType,
            SystemPrompt = Truncate(systemPrompt, MaxContentLength),
            UserPrompt = Truncate(userPrompt, MaxContentLength),
            RawResponse = Truncate(rawResponse, MaxContentLength),
            ParsedResultJson = parsedResultJson,
            ParseSucceeded = parseSucceeded,
            ErrorMessage = errorMessage,
            CreatedUtc = DateTime.UtcNow
        };

        await repository.CreateAsync(trace, cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...[truncated]");
}

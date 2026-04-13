using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// <see cref="IAgentExecutionTraceRecorder"/> that inserts rows via <see cref="IAgentExecutionTraceRepository"/>, truncating large prompt/response fields.
/// </summary>
public sealed class AgentExecutionTraceRecorder(
    IAgentExecutionTraceRepository repository,
    ILlmCostEstimator costEstimator,
    IOptions<LlmCostEstimationOptions> costOptions)
    : IAgentExecutionTraceRecorder
{
    private readonly IAgentExecutionTraceRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly ILlmCostEstimator _costEstimator =
        costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));

    private readonly IOptions<LlmCostEstimationOptions> _costOptions =
        costOptions ?? throw new ArgumentNullException(nameof(costOptions));

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
        AgentPromptReproMetadata? promptRepro = null,
        int? inputTokenCount = null,
        int? outputTokenCount = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        int inTok = inputTokenCount ?? 0;
        int outTok = outputTokenCount ?? 0;
        decimal? estimated = null;

        if (_costOptions.Value.Enabled && (inTok > 0 || outTok > 0))
            estimated = _costEstimator.EstimateUsd(inTok, outTok);

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
            PromptTemplateId = promptRepro?.TemplateId,
            PromptTemplateVersion = promptRepro?.TemplateVersion,
            SystemPromptContentSha256 = promptRepro?.SystemPromptContentSha256Hex,
            PromptReleaseLabel = promptRepro?.ReleaseLabel,
            InputTokenCount = inputTokenCount,
            OutputTokenCount = outputTokenCount,
            EstimatedCostUsd = estimated,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(trace, cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...[truncated]");
}

using System.Diagnostics;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// <see cref="IAgentExecutionTraceRecorder"/> that inserts rows via <see cref="IAgentExecutionTraceRepository"/>, truncating large prompt/response fields.
/// </summary>
public sealed class AgentExecutionTraceRecorder(
    IAgentExecutionTraceRepository repository,
    ILlmCostEstimator costEstimator,
    IOptions<LlmCostEstimationOptions> costOptions,
    IOptions<AgentExecutionTraceStorageOptions> traceStorageOptions,
    IArtifactBlobStore blobStore,
    IServiceScopeFactory scopeFactory,
    ILogger<AgentExecutionTraceRecorder> logger)
    : IAgentExecutionTraceRecorder
{
    private const string BlobContainerName = "agent-traces";

    private readonly IAgentExecutionTraceRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly ILlmCostEstimator _costEstimator =
        costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));

    private readonly IOptions<LlmCostEstimationOptions> _costOptions =
        costOptions ?? throw new ArgumentNullException(nameof(costOptions));

    private readonly IOptions<AgentExecutionTraceStorageOptions> _traceStorageOptions =
        traceStorageOptions ?? throw new ArgumentNullException(nameof(traceStorageOptions));

    private readonly IArtifactBlobStore _blobStore =
        blobStore ?? throw new ArgumentNullException(nameof(blobStore));

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    private readonly ILogger<AgentExecutionTraceRecorder> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

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
        string? modelDeploymentName = null,
        string? modelVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        int inTok = inputTokenCount ?? 0;
        int outTok = outputTokenCount ?? 0;
        decimal? estimated = null;

        if (_costOptions.Value.Enabled && (inTok > 0 || outTok > 0))
        {
            estimated = _costEstimator.EstimateUsd(inTok, outTok);
        }

        string resolvedDeployment = string.IsNullOrWhiteSpace(modelDeploymentName)
            ? AgentExecutionTraceModelMetadata.UnspecifiedDeploymentName
            : modelDeploymentName.Trim();

        string resolvedVersion = string.IsNullOrWhiteSpace(modelVersion)
            ? AgentExecutionTraceModelMetadata.UnspecifiedModelVersion
            : modelVersion.Trim();

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
            ModelDeploymentName = resolvedDeployment,
            ModelVersion = resolvedVersion,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(trace, cancellationToken);

        if (_traceStorageOptions.Value.PersistFullPrompts)
        {
            QueuePersistFullPrompts(trace.TraceId, runId, systemPrompt, userPrompt, rawResponse);
        }
    }

    private void QueuePersistFullPrompts(
        string traceId,
        string runId,
        string systemPrompt,
        string userPrompt,
        string rawResponse)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                IAgentExecutionTraceRepository repo = scope.ServiceProvider.GetRequiredService<IAgentExecutionTraceRepository>();

                string? systemKey = await WriteBlobWithRetryAsync(BlobContainerName, $"{runId}/{traceId}/system-prompt.txt", systemPrompt, traceId, "system_prompt");
                string? userKey = await WriteBlobWithRetryAsync(BlobContainerName, $"{runId}/{traceId}/user-prompt.txt", userPrompt, traceId, "user_prompt");
                string? responseKey = await WriteBlobWithRetryAsync(BlobContainerName, $"{runId}/{traceId}/response.txt", rawResponse, traceId, "response");

                bool anyFailed = systemKey is null || userKey is null || responseKey is null;

                await repo.PatchBlobStorageFieldsAsync(traceId, systemKey, userKey, responseKey, CancellationToken.None);

                if (anyFailed)
                {
                    await repo.PatchBlobUploadFailedAsync(traceId, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent trace full prompt persistence failed for TraceId={TraceId}", traceId);
            }
        });
    }

    private async Task<string?> WriteBlobWithRetryAsync(
        string containerName,
        string blobPath,
        string content,
        string traceId,
        string blobType)
    {
        const int maxAttempts = 3;
        const int retryDelayMs = 500;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _blobStore.WriteAsync(containerName, blobPath, content, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Agent trace {BlobType} blob write attempt {Attempt}/{MaxAttempts} failed for TraceId={TraceId}",
                    blobType,
                    attempt,
                    maxAttempts,
                    traceId);

                if (attempt < maxAttempts)
                {
                    await Task.Delay(retryDelayMs);
                }
            }
        }

        ArchLucidInstrumentation.AgentTraceBlobUploadFailuresTotal.Add(
            1,
            new TagList { { "agent_type", "unknown" }, { "blob_type", blobType } });

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...[truncated]");
}

using System.Diagnostics;
using System.Text.Json;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Repositories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     <see cref="IAgentExecutionTraceRecorder" /> that inserts rows via <see cref="IAgentExecutionTraceRepository" />,
///     truncating large prompt/response fields.
/// </summary>
public sealed class AgentExecutionTraceRecorder(
    IAgentExecutionTraceRepository repository,
    ILlmCostEstimator costEstimator,
    IOptions<LlmCostEstimationOptions> costOptions,
    IOptions<AgentExecutionTraceStorageOptions> traceStorageOptions,
    IArtifactBlobStore blobStore,
    IAuditService auditService,
    IScopeContextProvider scopeContextProvider,
    IOptionsMonitor<LlmPromptRedactionOptions> redactionOptions,
    IPromptRedactor promptRedactor,
    ILogger<AgentExecutionTraceRecorder> logger)
    : IAgentExecutionTraceRecorder
{
    private const string BlobContainerName = "agent-traces";

    /// <summary>Maximum stored length for prompt/response fields to prevent unbounded PII retention.</summary>
    private const int MaxContentLength = 8192;

    private const int MinBlobPersistenceTimeoutSeconds = 5;

    private const int MaxBlobPersistenceTimeoutSeconds = 300;

    private static readonly JsonSerializerOptions AuditJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IArtifactBlobStore _blobStore =
        blobStore ?? throw new ArgumentNullException(nameof(blobStore));

    private readonly ILlmCostEstimator _costEstimator =
        costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));

    private readonly IOptions<LlmCostEstimationOptions> _costOptions =
        costOptions ?? throw new ArgumentNullException(nameof(costOptions));

    private readonly ILogger<AgentExecutionTraceRecorder> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPromptRedactor _promptRedactor =
        promptRedactor ?? throw new ArgumentNullException(nameof(promptRedactor));

    private readonly IOptionsMonitor<LlmPromptRedactionOptions> _redactionOptions =
        redactionOptions ?? throw new ArgumentNullException(nameof(redactionOptions));

    private readonly IAgentExecutionTraceRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    private readonly IOptions<AgentExecutionTraceStorageOptions> _traceStorageOptions =
        traceStorageOptions ?? throw new ArgumentNullException(nameof(traceStorageOptions));

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
        bool isSimulatorExecution = false,
        string? failureReasonCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        int inTok = inputTokenCount ?? 0;
        int outTok = outputTokenCount ?? 0;
        decimal? estimated = null;

        if (_costOptions.Value.Enabled && (inTok > 0 || outTok > 0))

            estimated = _costEstimator.EstimateUsd(inTok, outTok);


        if (estimated is { } estUsd and > 0m)
        {
            ScopeContext costScope = _scopeContextProvider.GetCurrentScope();

            string tenantLabel = costScope.TenantId == Guid.Empty
                ? "unknown"
                : costScope.TenantId.ToString("N");

            ArchLucidInstrumentation.RecordLlmCostUsd(estUsd, tenantLabel);
        }

        string resolvedDeployment = string.IsNullOrWhiteSpace(modelDeploymentName)
            ? AgentExecutionTraceModelMetadata.UnspecifiedDeploymentName
            : modelDeploymentName.Trim();

        string resolvedVersion = string.IsNullOrWhiteSpace(modelVersion)
            ? AgentExecutionTraceModelMetadata.UnspecifiedModelVersion
            : modelVersion.Trim();

        string storeSystem = systemPrompt;
        string storeUser = userPrompt;
        string storeRaw = rawResponse;

        if (_redactionOptions.CurrentValue.Enabled)
        {
            PromptRedactionOutcome systemOutcome = _promptRedactor.Redact(systemPrompt);
            PromptRedactionOutcome userOutcome = _promptRedactor.Redact(userPrompt);
            PromptRedactionOutcome rawOutcome = _promptRedactor.Redact(rawResponse);
            storeSystem = systemOutcome.Text;
            storeUser = userOutcome.Text;
            storeRaw = rawOutcome.Text;
        }

        AgentExecutionTrace trace = new()
        {
            TraceId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            TaskId = taskId,
            AgentType = agentType,
            SystemPrompt = Truncate(storeSystem, MaxContentLength),
            UserPrompt = Truncate(storeUser, MaxContentLength),
            RawResponse = Truncate(storeRaw, MaxContentLength),
            ParsedResultJson = parsedResultJson,
            ParseSucceeded = parseSucceeded,
            ErrorMessage = errorMessage,
            FailureReasonCode = failureReasonCode,
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

        if (isSimulatorExecution)
            return;


        await PersistFullPromptsAsync(
            trace.TraceId,
            runId,
            agentType,
            storeSystem,
            storeUser,
            storeRaw,
            cancellationToken);
    }

    private async Task PersistFullPromptsAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        CancellationToken cancellationToken)
    {
        int timeoutSec = Math.Clamp(
            _traceStorageOptions.Value.BlobPersistenceTimeoutSeconds,
            MinBlobPersistenceTimeoutSeconds,
            MaxBlobPersistenceTimeoutSeconds);

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(timeoutSec));

        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        CancellationToken blobCt = linked.Token;

        Stopwatch sw = Stopwatch.StartNew();

        string agentLabel = agentType.ToString();

        TagList agentTags = new() { { "agent_type", agentLabel } };

        bool timedOut = false;

        string? systemKey = null;

        string? userKey = null;

        string? responseKey = null;

        try
        {
            systemKey = await WriteBlobWithRetryAsync(
                BlobContainerName,
                $"{runId}/{traceId}/system-prompt.txt",
                systemPrompt,
                traceId,
                "system_prompt",
                agentType,
                blobCt);

            userKey = await WriteBlobWithRetryAsync(
                BlobContainerName,
                $"{runId}/{traceId}/user-prompt.txt",
                userPrompt,
                traceId,
                "user_prompt",
                agentType,
                blobCt);

            responseKey = await WriteBlobWithRetryAsync(
                BlobContainerName,
                $"{runId}/{traceId}/response.txt",
                rawResponse,
                traceId,
                "response",
                agentType,
                blobCt);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;


            timedOut = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Agent trace full prompt persistence failed for TraceId={TraceId}",
                LogSanitizer.Sanitize(traceId));

            List<string> failedOnException = BuildFailedBlobTypes(systemKey, userKey, responseKey);

            await TryLogBlobPersistenceAuditAsync(
                traceId,
                runId,
                agentType,
                "exception",
                failedOnException,
                CancellationToken.None);

            await _repository.PatchBlobStorageFieldsAsync(traceId, systemKey, userKey, responseKey,
                CancellationToken.None);

            await _repository.PatchBlobUploadFailedAsync(traceId, true, CancellationToken.None);

            await ApplyMandatoryInlineAndVerifyAsync(
                traceId,
                runId,
                agentType,
                systemPrompt,
                userPrompt,
                rawResponse,
                systemKey,
                userKey,
                responseKey,
                sw,
                agentTags,
                CancellationToken.None);

            return;
        }

        bool anyFailed = timedOut || systemKey is null || userKey is null || responseKey is null;

        await _repository.PatchBlobStorageFieldsAsync(traceId, systemKey, userKey, responseKey, CancellationToken.None);

        if (anyFailed)
        {
            await _repository.PatchBlobUploadFailedAsync(traceId, true, CancellationToken.None);

            List<string> failed = BuildFailedBlobTypes(systemKey, userKey, responseKey);

            string reason = timedOut ? "timeout" : "upload_failed";

            await TryLogBlobPersistenceAuditAsync(traceId, runId, agentType, reason, failed, CancellationToken.None);

            await ApplyMandatoryInlineAndVerifyAsync(
                traceId,
                runId,
                agentType,
                systemPrompt,
                userPrompt,
                rawResponse,
                systemKey,
                userKey,
                responseKey,
                sw,
                agentTags,
                CancellationToken.None);
        }
        else
        {
            await _repository.PatchBlobUploadFailedAsync(traceId, false, CancellationToken.None);

            await VerifyMandatoryForensicCoverageAsync(
                traceId,
                runId,
                agentType,
                systemPrompt,
                userPrompt,
                rawResponse,
                CancellationToken.None);

            ArchLucidInstrumentation.AgentTraceBlobPersistDurationMs.Record(sw.Elapsed.TotalMilliseconds, agentTags);
        }
    }

    private async Task ApplyMandatoryInlineAndVerifyAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        string? systemKey,
        string? userKey,
        string? responseKey,
        Stopwatch sw,
        TagList agentTags,
        CancellationToken cancellationToken)
    {
        try
        {
            await TryPatchInlineForMissingBlobsAsync(
                traceId,
                systemKey,
                userKey,
                responseKey,
                systemPrompt,
                userPrompt,
                rawResponse,
                agentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Agent trace mandatory inline SQL fallback threw for TraceId={TraceId}",
                LogSanitizer.Sanitize(traceId));

            await MarkInlineForensicFailureAsync(
                traceId,
                runId,
                agentType,
                "inline_sql_patch_exception",
                ex.Message,
                cancellationToken);

            ArchLucidInstrumentation.AgentTraceBlobPersistDurationMs.Record(sw.Elapsed.TotalMilliseconds, agentTags);

            return;
        }

        await VerifyMandatoryForensicCoverageAsync(
            traceId,
            runId,
            agentType,
            systemPrompt,
            userPrompt,
            rawResponse,
            cancellationToken);

        ArchLucidInstrumentation.AgentTraceBlobPersistDurationMs.Record(sw.Elapsed.TotalMilliseconds, agentTags);
    }

    private static bool ForensicPartStored(string content, string? blobKey, string? inline)
    {
        return string.IsNullOrEmpty(content)
               || !string.IsNullOrEmpty(blobKey)
               || !string.IsNullOrEmpty(inline);
    }

    private async Task VerifyMandatoryForensicCoverageAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        CancellationToken cancellationToken)
    {
        AgentExecutionTrace? row = await _repository.GetByTraceIdAsync(traceId, cancellationToken);

        if (row is null)
        {
            await MarkInlineForensicFailureAsync(
                traceId,
                runId,
                agentType,
                "trace_row_missing",
                null,
                cancellationToken);

            return;
        }

        if (!ForensicPartStored(systemPrompt, row.FullSystemPromptBlobKey, row.FullSystemPromptInline)
            || !ForensicPartStored(userPrompt, row.FullUserPromptBlobKey, row.FullUserPromptInline)
            || !ForensicPartStored(rawResponse, row.FullResponseBlobKey, row.FullResponseInline))

            await MarkInlineForensicFailureAsync(
                traceId,
                runId,
                agentType,
                "mandatory_full_text_incomplete",
                null,
                cancellationToken);
    }

    private async Task MarkInlineForensicFailureAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string reason,
        string? exceptionDetail,
        CancellationToken cancellationToken)
    {
        await _repository.PatchInlineFallbackFailedAsync(traceId, true, cancellationToken);

        await TryLogInlineFallbackFailedAuditAsync(
            traceId,
            runId,
            agentType,
            reason,
            exceptionDetail,
            cancellationToken);
    }

    private async Task TryLogInlineFallbackFailedAuditAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string reason,
        string? exceptionDetail,
        CancellationToken cancellationToken)
    {
        try
        {
            ScopeContext scope = _scopeContextProvider.GetCurrentScope();

            Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;

            string dataJson = JsonSerializer.Serialize(
                new
                {
                    traceId,
                    runId,
                    agentType = agentType.ToString(),
                    reason,
                    exceptionDetail
                },
                AuditJsonOptions);

            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.AgentTraceInlineFallbackFailed,
                ActorUserId = "agent-runtime",
                ActorUserName = "agent-runtime",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = dataJson
            };

            await _auditService.LogAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Durable audit for AgentTraceInlineFallbackFailed failed for TraceId={TraceId}",
                LogSanitizer.Sanitize(traceId));
        }
    }

    private Task TryPatchInlineForMissingBlobsAsync(
        string traceId,
        string? systemKey,
        string? userKey,
        string? responseKey,
        string systemPrompt,
        string userPrompt,
        string rawResponse,
        AgentType agentType,
        CancellationToken cancellationToken)
    {
        string? systemInline = systemKey is null ? systemPrompt : null;

        string? userInline = userKey is null ? userPrompt : null;

        string? responseInline = responseKey is null ? rawResponse : null;

        if (systemInline is not null)

            RecordPromptInlineFallback(agentType, "system_prompt");


        if (userInline is not null)

            RecordPromptInlineFallback(agentType, "user_prompt");


        if (responseInline is not null)

            RecordPromptInlineFallback(agentType, "response");


        if (systemInline is null && userInline is null && responseInline is null)
            return Task.CompletedTask;


        return _repository.PatchInlinePromptFallbackAsync(
            traceId,
            systemInline,
            userInline,
            responseInline,
            cancellationToken);
    }

    private static void RecordPromptInlineFallback(AgentType agentType, string blobType)
    {
        TagList tags = new() { { "agent_type", agentType.ToString() }, { "blob_type", blobType } };

        ArchLucidInstrumentation.AgentTracePromptInlineFallbacksTotal.Add(1, tags);
    }

    private async Task TryLogBlobPersistenceAuditAsync(
        string traceId,
        string runId,
        AgentType agentType,
        string reason,
        IReadOnlyList<string> failedBlobTypes,
        CancellationToken cancellationToken)
    {
        try
        {
            ScopeContext scope = _scopeContextProvider.GetCurrentScope();

            Guid? runGuid = Guid.TryParse(runId, out Guid rid) ? rid : null;

            string dataJson = JsonSerializer.Serialize(
                new
                {
                    traceId,
                    runId,
                    agentType = agentType.ToString(),
                    reason,
                    failedBlobTypes
                },
                AuditJsonOptions);

            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.AgentTraceBlobPersistenceFailed,
                ActorUserId = "agent-runtime",
                ActorUserName = "agent-runtime",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runGuid,
                DataJson = dataJson
            };

            await _auditService.LogAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Durable audit for AgentTraceBlobPersistenceFailed failed for TraceId={TraceId}",
                LogSanitizer.Sanitize(traceId));
        }
    }

    private async Task<string?> WriteBlobWithRetryAsync(
        string containerName,
        string blobPath,
        string content,
        string traceId,
        string blobType,
        AgentType agentType,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        // Fixed backoff between attempts (Prompt 2 / quality spec): 2 retries after the first try, 500 ms apart.
        const int retryDelayMs = 500;

        string agentLabel = agentType.ToString();

        TagList tags = new() { { "agent_type", agentLabel }, { "blob_type", blobType } };

        for (int attempt = 1; attempt <= maxAttempts; attempt++)

            try
            {
                return await _blobStore.WriteAsync(containerName, blobPath, content, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Agent trace {BlobType} blob write attempt {Attempt}/{MaxAttempts} failed for TraceId={TraceId}",
                    LogSanitizer.Sanitize(blobType),
                    attempt,
                    maxAttempts,
                    LogSanitizer.Sanitize(traceId));

                if (attempt < maxAttempts)

                    await Task.Delay(retryDelayMs, cancellationToken);
            }


        ArchLucidInstrumentation.AgentTraceBlobUploadFailuresTotal.Add(1, tags);

        return null;
    }

    private static List<string> BuildFailedBlobTypes(string? systemKey, string? userKey, string? responseKey)
    {
        List<string> failed = [];

        if (systemKey is null)

            failed.Add("system_prompt");


        if (userKey is null)

            failed.Add("user_prompt");


        if (responseKey is null)

            failed.Add("response");


        return failed;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...[truncated]");
    }
}

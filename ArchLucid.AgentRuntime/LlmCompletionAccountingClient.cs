using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Scoped decorator: enforces per-tenant token quota, records OTel counters (and optional per-tenant series),
/// and forwards to the inner client (typically <see cref="AzureOpenAiCompletionClient"/>).
/// </summary>
public sealed class LlmCompletionAccountingClient(
    IAgentCompletionClient inner,
    LlmTokenQuotaWindowTracker quotaTracker,
    IScopeContextProvider scopeProvider,
    IOptionsMonitor<LlmTokenQuotaOptions> quotaOptions,
    IOptionsMonitor<LlmTelemetryOptions> telemetryOptions,
    IOptionsMonitor<LlmTelemetryLabelOptions> labelOptions,
    IUsageMeteringService usageMetering,
    ILogger<LlmCompletionAccountingClient> logger)
    : IAgentCompletionClient
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly LlmTokenQuotaWindowTracker _quotaTracker = quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));
    private readonly IScopeContextProvider _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    private readonly IOptionsMonitor<LlmTokenQuotaOptions> _quotaOptions = quotaOptions ?? throw new ArgumentNullException(nameof(quotaOptions));
    private readonly IOptionsMonitor<LlmTelemetryOptions> _telemetryOptions = telemetryOptions ?? throw new ArgumentNullException(nameof(telemetryOptions));
    private readonly IOptionsMonitor<LlmTelemetryLabelOptions> _labelOptions = labelOptions ?? throw new ArgumentNullException(nameof(labelOptions));
    private readonly IUsageMeteringService _usageMetering = usageMetering ?? throw new ArgumentNullException(nameof(usageMetering));
    private readonly ILogger<LlmCompletionAccountingClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _inner.Descriptor;

    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        if (_quotaOptions.CurrentValue.Enabled)
        {
            _quotaTracker.EnsureWithinQuotaBeforeCall(scope.TenantId);
        }

        try
        {
            return await _inner.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
        }
        finally
        {
            if (AzureOpenAiCompletionClient.TryConsumeLastCompletionTokenUsage(out int promptTok, out int completionTok))
            {
                _quotaTracker.RecordUsage(scope.TenantId, promptTok, completionTok);

                bool perTenant = _telemetryOptions.CurrentValue.RecordPerTenantTokens;
                string? tenantKey = perTenant && scope.TenantId != Guid.Empty ? scope.TenantId.ToString("N") : null;

                LlmTelemetryLabelOptions labels = _labelOptions.CurrentValue;

                ArchLucidInstrumentation.RecordLlmTokenUsage(
                    promptTok,
                    completionTok,
                    perTenant,
                    tenantKey,
                    labels.ProviderId,
                    labels.ModelDeploymentLabel);

                _ = TryRecordLlmUsageMeteringAsync(scope, promptTok, completionTok, cancellationToken);
            }
        }
    }

    private async Task TryRecordLlmUsageMeteringAsync(
        ScopeContext scope,
        int promptTok,
        int completionTok,
        CancellationToken cancellationToken)
    {
        if (scope.TenantId == Guid.Empty)
            return;

        DateTimeOffset recordedUtc = DateTimeOffset.UtcNow;
        string? correlationId = System.Diagnostics.Activity.Current?.Id;

        try
        {
            if (promptTok > 0)
            {
                await _usageMetering
                    .RecordAsync(
                        new UsageEvent
                        {
                            TenantId = scope.TenantId,
                            WorkspaceId = scope.WorkspaceId,
                            ProjectId = scope.ProjectId,
                            Kind = UsageMeterKind.LlmPromptTokens,
                            Quantity = promptTok,
                            RecordedUtc = recordedUtc,
                            CorrelationId = correlationId,
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (completionTok > 0)
            {
                await _usageMetering
                    .RecordAsync(
                        new UsageEvent
                        {
                            TenantId = scope.TenantId,
                            WorkspaceId = scope.WorkspaceId,
                            ProjectId = scope.ProjectId,
                            Kind = UsageMeterKind.LlmCompletionTokens,
                            Quantity = completionTok,
                            RecordedUtc = recordedUtc,
                            CorrelationId = correlationId,
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    ex,
                    "Usage metering failed for tenant {TenantId} (LLM tokens).",
                    scope.TenantId);
            }
        }
    }
}

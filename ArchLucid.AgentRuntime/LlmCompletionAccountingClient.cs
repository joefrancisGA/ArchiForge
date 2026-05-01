using System.Diagnostics;

using ArchLucid.Core;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Scoped decorator: enforces per-tenant token quota, records OTel counters (and optional per-tenant series),
///     and forwards to the inner client (typically <see cref="AzureOpenAiCompletionClient" />).
/// </summary>
public sealed class LlmCompletionAccountingClient(
    IAgentCompletionClient inner,
    LlmTokenQuotaWindowTracker quotaTracker,
    IScopeContextProvider scopeProvider,
    IOptionsMonitor<LlmTokenQuotaOptions> quotaOptions,
    IOptionsMonitor<LlmTelemetryOptions> telemetryOptions,
    IOptionsMonitor<LlmTelemetryLabelOptions> labelOptions,
    IOptionsMonitor<LlmPromptRedactionOptions> redactionOptions,
    IPromptRedactor promptRedactor,
    IUsageMeteringService usageMetering,
    IOptionsMonitor<LlmDailyTenantBudgetOptions> dailyTenantBudgetOptions,
    LlmDailyTenantBudgetTracker dailyTenantBudgetTracker,
    IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions> monthlyDollarBudgetOptions,
    LlmMonthlyTenantDollarBudgetTracker monthlyDollarBudgetTracker,
    IAuditService auditService,
    ILogger<LlmCompletionAccountingClient> logger)
    : IAgentCompletionClient
{
    private readonly IAgentCompletionClient _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IOptionsMonitor<LlmTelemetryLabelOptions> _labelOptions =
        labelOptions ?? throw new ArgumentNullException(nameof(labelOptions));

    private readonly ILogger<LlmCompletionAccountingClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPromptRedactor _promptRedactor =
        promptRedactor ?? throw new ArgumentNullException(nameof(promptRedactor));

    private readonly IOptionsMonitor<LlmTokenQuotaOptions> _quotaOptions =
        quotaOptions ?? throw new ArgumentNullException(nameof(quotaOptions));

    private readonly LlmTokenQuotaWindowTracker _quotaTracker =
        quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));

    private readonly IOptionsMonitor<LlmPromptRedactionOptions> _redactionOptions =
        redactionOptions ?? throw new ArgumentNullException(nameof(redactionOptions));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IOptionsMonitor<LlmTelemetryOptions> _telemetryOptions =
        telemetryOptions ?? throw new ArgumentNullException(nameof(telemetryOptions));

    private readonly IUsageMeteringService _usageMetering =
        usageMetering ?? throw new ArgumentNullException(nameof(usageMetering));

    private readonly IOptionsMonitor<LlmDailyTenantBudgetOptions> _dailyTenantBudgetOptions =
        dailyTenantBudgetOptions ?? throw new ArgumentNullException(nameof(dailyTenantBudgetOptions));

    private readonly LlmDailyTenantBudgetTracker _dailyTenantBudgetTracker =
        dailyTenantBudgetTracker ?? throw new ArgumentNullException(nameof(dailyTenantBudgetTracker));

    private readonly IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions> _monthlyDollarBudgetOptions =
        monthlyDollarBudgetOptions ?? throw new ArgumentNullException(nameof(monthlyDollarBudgetOptions));

    private readonly LlmMonthlyTenantDollarBudgetTracker _monthlyDollarBudgetTracker =
        monthlyDollarBudgetTracker ?? throw new ArgumentNullException(nameof(monthlyDollarBudgetTracker));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _inner.Descriptor;

    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        string providerKind = _inner.Descriptor.ProviderKind;

        try
        {
            if (_dailyTenantBudgetOptions.CurrentValue.Enabled)
                _dailyTenantBudgetTracker.EnsureWithinBudgetBeforeCall(scope.TenantId, providerKind);

            if (_monthlyDollarBudgetOptions.CurrentValue.Enabled)
                _monthlyDollarBudgetTracker.EnsureWithinBudgetBeforeCall(scope.TenantId, providerKind);

            if (_quotaOptions.CurrentValue.Enabled)
                _quotaTracker.EnsureWithinQuotaBeforeCall(scope.TenantId);
        }
        catch (LlmTokenQuotaExceededException)
        {
            ArchLucidInstrumentation.LlmQuotaExceededTotal.Add(1);
            throw;
        }


        LlmPromptRedactionOptions redactionOpts = _redactionOptions.CurrentValue;
        string outboundSystem = systemPrompt;
        string outboundUser = userPrompt;

        if (!redactionOpts.Enabled)
        {
            ArchLucidInstrumentation.RecordLlmPromptRedactionSkipped();
        }
        else
        {
            PromptRedactionOutcome systemOutcome = _promptRedactor.Redact(systemPrompt);
            PromptRedactionOutcome userOutcome = _promptRedactor.Redact(userPrompt);

            foreach (KeyValuePair<string, int> kv in systemOutcome.CountsByCategory)
                ArchLucidInstrumentation.RecordLlmPromptRedactions(kv.Key, kv.Value);

            foreach (KeyValuePair<string, int> kv in userOutcome.CountsByCategory)
                ArchLucidInstrumentation.RecordLlmPromptRedactions(kv.Key, kv.Value);

            outboundSystem = systemOutcome.Text;
            outboundUser = userOutcome.Text;
        }

        try
        {
            return await _inner.CompleteJsonAsync(outboundSystem, outboundUser, cancellationToken);
        }
        finally
        {
            if (AzureOpenAiCompletionClient.TryConsumeLastCompletionTokenUsage(out int promptTok,
                    out int completionTok))
            {
                _quotaTracker.RecordUsage(scope.TenantId, promptTok, completionTok);

                _dailyTenantBudgetTracker.RecordUsageAndMaybeWarn(
                    scope.TenantId,
                    providerKind,
                    _scopeProvider,
                    _auditService,
                    promptTok,
                    completionTok);

                _monthlyDollarBudgetTracker.RecordUsageAndMaybeWarn(
                    scope.TenantId,
                    providerKind,
                    _scopeProvider,
                    _auditService,
                    promptTok,
                    completionTok);

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
        string? correlationId = Activity.Current?.Id;

        try
        {
            if (promptTok > 0)

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
                            CorrelationId = correlationId
                        },
                        cancellationToken)
                    .ConfigureAwait(false);


            if (completionTok > 0)

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
                            CorrelationId = correlationId
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    ex,
                    "Usage metering failed for tenant {TenantId} (LLM tokens).",
                    scope.TenantId);
        }
    }
}

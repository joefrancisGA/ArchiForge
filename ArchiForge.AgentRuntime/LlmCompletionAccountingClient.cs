using ArchiForge.Core.Configuration;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Core.Scoping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Scoped decorator: enforces per-tenant token quota, records OTel counters (and optional per-tenant series),
/// and forwards to the inner client (typically <see cref="AzureOpenAiCompletionClient"/>).
/// </summary>
public sealed class LlmCompletionAccountingClient : IAgentCompletionClient
{
    private readonly IAgentCompletionClient _inner;
    private readonly LlmTokenQuotaWindowTracker _quotaTracker;
    private readonly IScopeContextProvider _scopeProvider;
    private readonly IOptionsMonitor<LlmTokenQuotaOptions> _quotaOptions;
    private readonly IOptionsMonitor<LlmTelemetryOptions> _telemetryOptions;
    private readonly IOptionsMonitor<LlmTelemetryLabelOptions> _labelOptions;
    private readonly ILogger<LlmCompletionAccountingClient> _logger;

    public LlmCompletionAccountingClient(
        IAgentCompletionClient inner,
        LlmTokenQuotaWindowTracker quotaTracker,
        IScopeContextProvider scopeProvider,
        IOptionsMonitor<LlmTokenQuotaOptions> quotaOptions,
        IOptionsMonitor<LlmTelemetryOptions> telemetryOptions,
        IOptionsMonitor<LlmTelemetryLabelOptions> labelOptions,
        ILogger<LlmCompletionAccountingClient> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _quotaTracker = quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _quotaOptions = quotaOptions ?? throw new ArgumentNullException(nameof(quotaOptions));
        _telemetryOptions = telemetryOptions ?? throw new ArgumentNullException(nameof(telemetryOptions));
        _labelOptions = labelOptions ?? throw new ArgumentNullException(nameof(labelOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

                ArchiForgeInstrumentation.RecordLlmTokenUsage(
                    promptTok,
                    completionTok,
                    perTenant,
                    tenantKey,
                    labels.ProviderId,
                    labels.ModelDeploymentLabel);
            }
        }
    }
}

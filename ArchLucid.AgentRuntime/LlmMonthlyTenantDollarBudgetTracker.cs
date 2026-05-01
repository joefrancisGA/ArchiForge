using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

using ArchLucid.Core;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>UTC-month estimated USD spend per tenant for <see cref="LlmMonthlyTenantDollarBudgetOptions" /> (warn once, hard stop).</summary>
public sealed class LlmMonthlyTenantDollarBudgetTracker(
    IOptionsMonitor<LlmMonthlyTenantDollarBudgetOptions> optionsMonitor,
    ILlmCostEstimator costEstimator)
{
    private readonly ConcurrentDictionary<Guid, TenantMonthState> _states = new();

    /// <summary>Throws <see cref="LlmTokenQuotaExceededException" /> when the next call would exceed the UTC-month hard cutoff.</summary>
    public void EnsureWithinBudgetBeforeCall(Guid tenantId, string providerKind)
    {
        if (tenantId == Guid.Empty || IsExcludedProvider(providerKind))
            return;

        LlmMonthlyTenantDollarBudgetOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || opts.HardCutoffUsdPerUtcMonth < 0.01m)
            return;

        int assumedPrompt = Math.Clamp(opts.AssumedMaxPromptTokensPerRequest, 1, 1_000_000);
        int assumedCompletion = Math.Clamp(opts.AssumedMaxCompletionTokensPerRequest, 1, 262_144);
        decimal? assumedUsd = costEstimator.EstimateUsd(assumedPrompt, assumedCompletion);
        decimal assumed = assumedUsd ?? 0m;

        if (assumed <= 0m)
            return;

        TenantMonthState state = GetOrCreateState(tenantId);
        (int year, int month) = GetUtcYearMonth();

        lock (state.Sync)
        {
            ResetIfNewUtcMonthLocked(state, year, month);

            if (state.SpentUsd + assumed <= opts.HardCutoffUsdPerUtcMonth)
                return;

            DateTimeOffset retryAfterUtc = FirstInstantOfNextUtcMonth(year, month);

            throw new LlmTokenQuotaExceededException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "LLM monthly dollar budget exceeded for tenant (UTC month hard cap {0:C}, used ~{1:C}).",
                    opts.HardCutoffUsdPerUtcMonth,
                    state.SpentUsd),
                retryAfterUtc);
        }
    }

    /// <summary>Accumulates estimated USD and fires the once-per-UTC-month warning audit when crossing the warn threshold.</summary>
    public void RecordUsageAndMaybeWarn(
        Guid tenantId,
        string providerKind,
        IScopeContextProvider scopeProvider,
        IAuditService? auditService,
        int promptTokens,
        int completionTokens)
    {
        if (tenantId == Guid.Empty || IsExcludedProvider(providerKind))
            return;

        LlmMonthlyTenantDollarBudgetOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || opts.IncludedUsdPerUtcMonth < 0.01m || opts.HardCutoffUsdPerUtcMonth < 0.01m)
            return;

        if (promptTokens < 1 && completionTokens < 1)
            return;

        decimal? addUsd = costEstimator.EstimateUsd(promptTokens, completionTokens);

        if (addUsd is null or <= 0m)
            return;

        decimal warnAt = decimal.Round(
            opts.IncludedUsdPerUtcMonth * decimal.Clamp(opts.WarnFraction, 0.01m, 0.99m),
            4,
            MidpointRounding.AwayFromZero);

        TenantMonthState state = GetOrCreateState(tenantId);
        (int year, int month) = GetUtcYearMonth();
        bool shouldAudit = false;
        decimal newTotal;

        lock (state.Sync)
        {
            ResetIfNewUtcMonthLocked(state, year, month);
            decimal before = state.SpentUsd;
            state.SpentUsd += addUsd.Value;
            newTotal = state.SpentUsd;

            if (!state.WarnedApproaching && before < warnAt && newTotal >= warnAt)
            {
                state.WarnedApproaching = true;
                shouldAudit = true;
            }
        }

        if (!shouldAudit || auditService is null)
            return;

        try
        {
            ScopeContext scope = scopeProvider.GetCurrentScope();
            string monthLabel = string.Format(CultureInfo.InvariantCulture, "{0:0000}-{1:00}", year, month);
            string dataJson = JsonSerializer.Serialize(
                new
                {
                    utcMonth = monthLabel,
                    spentUsd = newTotal,
                    warnAtUsd = warnAt,
                    includedUsd = opts.IncludedUsdPerUtcMonth,
                    hardCutoffUsd = opts.HardCutoffUsdPerUtcMonth
                });

            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.LlmTenantMonthlyDollarBudgetApproaching,
                ActorUserId = "llm-monthly-dollar-budget",
                ActorUserName = "llm-monthly-dollar-budget",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = dataJson
            };

            _ = auditService.LogAsync(auditEvent, CancellationToken.None).ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
        catch
        {
            // Never block completion path on audit scheduling.
        }
    }

    private static bool IsExcludedProvider(string providerKind)
    {
        if (string.IsNullOrWhiteSpace(providerKind))
            return false;

        return string.Equals(providerKind, "simulator", StringComparison.OrdinalIgnoreCase)
               || string.Equals(providerKind, "fake", StringComparison.OrdinalIgnoreCase)
               || string.Equals(providerKind, "echo", StringComparison.OrdinalIgnoreCase);
    }

    private TenantMonthState GetOrCreateState(Guid tenantId) => _states.GetOrAdd(tenantId, _ => new TenantMonthState());

    private static void ResetIfNewUtcMonthLocked(TenantMonthState state, int year, int month)
    {
        if (state.UtcYear == year && state.UtcMonth == month)
            return;

        state.UtcYear = year;
        state.UtcMonth = month;
        state.SpentUsd = 0m;
        state.WarnedApproaching = false;
    }

    private static (int Year, int Month) GetUtcYearMonth()
    {
        DateTime utc = DateTime.UtcNow;

        return (utc.Year, utc.Month);
    }

    private static DateTimeOffset FirstInstantOfNextUtcMonth(int year, int month)
    {
        DateTime firstNext = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        return new DateTimeOffset(firstNext, TimeSpan.Zero);
    }

    private sealed class TenantMonthState
    {
        public object Sync { get; } = new();

        public int UtcYear
        {
            get; set;
        }

        public int UtcMonth
        {
            get; set;
        }

        public decimal SpentUsd
        {
            get; set;
        }

        public bool WarnedApproaching
        {
            get; set;
        }
    }
}

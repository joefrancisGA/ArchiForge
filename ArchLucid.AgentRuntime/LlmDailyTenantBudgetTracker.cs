using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>UTC-day combined token totals per tenant for <see cref="LlmDailyTenantBudgetOptions" /> (warn once, hard stop).</summary>
public sealed class LlmDailyTenantBudgetTracker(IOptionsMonitor<LlmDailyTenantBudgetOptions> optionsMonitor)
{
    private readonly ConcurrentDictionary<Guid, TenantDayState> _states = new();

    /// <summary>Throws <see cref="LlmTokenQuotaExceededException" /> when the next call would exceed the UTC-day cap.</summary>
    public void EnsureWithinBudgetBeforeCall(Guid tenantId, string providerKind)
    {
        if (tenantId == Guid.Empty || IsExcludedProvider(providerKind))
            return;

        LlmDailyTenantBudgetOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || opts.MaxTotalTokensPerTenantPerUtcDay < 1)
            return;

        TenantDayState state = GetOrCreateState(tenantId);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        long max = opts.MaxTotalTokensPerTenantPerUtcDay;
        int assumed = Math.Clamp(opts.AssumedMaxTotalTokensPerRequest, 1, 2_000_000);

        lock (state.Sync)
        {
            ResetIfNewUtcDayLocked(state, today);

            if (state.TotalTokens + assumed > max)
                throw new LlmTokenQuotaExceededException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "LLM daily token budget exceeded for tenant (UTC day cap {0}, used ~{1}).",
                        max,
                        state.TotalTokens));
        }
    }

    /// <summary>Accumulates usage and fires the once-per-UTC-day warning audit when crossing the warn threshold.</summary>
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

        LlmDailyTenantBudgetOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || opts.MaxTotalTokensPerTenantPerUtcDay < 1)
            return;

        if (promptTokens < 1 && completionTokens < 1)
            return;

        long added = (long)Math.Max(0, promptTokens) + Math.Max(0, completionTokens);
        TenantDayState state = GetOrCreateState(tenantId);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        long max = opts.MaxTotalTokensPerTenantPerUtcDay;
        long warnAt = (long)Math.Floor(max * (double)decimal.Clamp(opts.WarnFraction, 0.01m, 0.99m));

        bool shouldAudit = false;
        long newTotal;

        lock (state.Sync)
        {
            ResetIfNewUtcDayLocked(state, today);
            long before = state.TotalTokens;
            state.TotalTokens += added;
            newTotal = state.TotalTokens;

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
            string dataJson = JsonSerializer.Serialize(
                new
                {
                    utcDay = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    usedTotal = newTotal,
                    warnAt,
                    maxTotal = max
                });

            AuditEvent auditEvent = new()
            {
                EventType = AuditEventTypes.LlmTenantDailyBudgetApproaching,
                ActorUserId = "llm-daily-budget",
                ActorUserName = "llm-daily-budget",
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

    private TenantDayState GetOrCreateState(Guid tenantId) => _states.GetOrAdd(tenantId, _ => new TenantDayState());

    private static void ResetIfNewUtcDayLocked(TenantDayState state, DateOnly today)
    {
        if (state.UtcDay == today)
            return;

        state.UtcDay = today;
        state.TotalTokens = 0;
        state.WarnedApproaching = false;
    }

    private sealed class TenantDayState
    {
        public object Sync { get; } = new();

        public DateOnly UtcDay { get; set; }

        public long TotalTokens { get; set; }

        public bool WarnedApproaching { get; set; }
    }
}

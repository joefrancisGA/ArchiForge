using System.Collections.Concurrent;

using ArchLucid.Core;
using ArchLucid.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>Thread-safe sliding-window token totals per tenant for <see cref="LlmTokenQuotaOptions" />.</summary>
public sealed class LlmTokenQuotaWindowTracker(IOptionsMonitor<LlmTokenQuotaOptions> optionsMonitor)
{
    private readonly ConcurrentDictionary<Guid, TenantWindow> _windows = new();

    /// <summary>Throws <see cref="LlmTokenQuotaExceededException" /> if the next call could exceed configured limits.</summary>
    public void EnsureWithinQuotaBeforeCall(Guid tenantId)
    {
        LlmTokenQuotaOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || tenantId == Guid.Empty)
            return;


        if (opts is { MaxPromptTokensPerTenantPerWindow: < 1, MaxCompletionTokensPerTenantPerWindow: < 1 })
            return;


        TenantWindow window = _windows.GetOrAdd(tenantId, _ => new TenantWindow());
        DateTime utcNow = DateTime.UtcNow;
        TimeSpan windowLength = TimeSpan.FromMinutes(Math.Clamp(opts.WindowMinutes, 1, 1440));

        lock (window.Sync)
        {
            PruneLocked(window, utcNow, windowLength);

            long promptSum = SumPromptLocked(window);
            long completionSum = SumCompletionLocked(window);

            if (opts.MaxPromptTokensPerTenantPerWindow > 0 &&
                promptSum + opts.AssumedMaxPromptTokensPerRequest > opts.MaxPromptTokensPerTenantPerWindow)

                throw new LlmTokenQuotaExceededException(
                    $"LLM prompt token quota exceeded for tenant (window {opts.WindowMinutes}m, limit {opts.MaxPromptTokensPerTenantPerWindow}).");


            if (opts.MaxCompletionTokensPerTenantPerWindow > 0 &&
                completionSum + opts.AssumedMaxCompletionTokensPerRequest > opts.MaxCompletionTokensPerTenantPerWindow)

                throw new LlmTokenQuotaExceededException(
                    $"LLM completion token quota exceeded for tenant (window {opts.WindowMinutes}m, limit {opts.MaxCompletionTokensPerTenantPerWindow}).");
        }
    }

    /// <summary>Records usage after a successful Azure OpenAI completion (skipped when tenant is empty or quota disabled).</summary>
    public void RecordUsage(Guid tenantId, int promptTokens, int completionTokens)
    {
        LlmTokenQuotaOptions opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled || tenantId == Guid.Empty)
            return;


        if (promptTokens < 1 && completionTokens < 1)
            return;


        TenantWindow window = _windows.GetOrAdd(tenantId, _ => new TenantWindow());
        DateTime utcNow = DateTime.UtcNow;
        TimeSpan windowLength = TimeSpan.FromMinutes(Math.Clamp(opts.WindowMinutes, 1, 1440));

        lock (window.Sync)
        {
            PruneLocked(window, utcNow, windowLength);
            window.Events.Add((utcNow, Math.Max(0, promptTokens), Math.Max(0, completionTokens)));
        }
    }

    private static void PruneLocked(TenantWindow window, DateTime utcNow, TimeSpan windowLength)
    {
        DateTime cutoff = utcNow - windowLength;

        for (int i = window.Events.Count - 1; i >= 0; i--)

            if (window.Events[i].Utc < cutoff)

                window.Events.RemoveAt(i);
    }

    private static long SumPromptLocked(TenantWindow window)
    {
        return window.Events.Sum(e => (long)e.PromptTokens);
    }

    private static long SumCompletionLocked(TenantWindow window)
    {
        return window.Events.Sum(e => (long)e.CompletionTokens);
    }

    private sealed class TenantWindow
    {
        public object Sync
        {
            get;
        } = new();

        public List<(DateTime Utc, int PromptTokens, int CompletionTokens)> Events
        {
            get;
        } = [];
    }
}

using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Tenancy;
/// <summary>UTC trial lifecycle transitions (see <c>docs/go-to-market/TRIAL_AND_SIGNUP.md</c> §3).</summary>
public static class TrialLifecyclePolicy
{
    public static ArchLucid.Application.Tenancy.TrialLifecycleAdvancement? TryGetNextAdvancement(TenantRecord tenant, DateTimeOffset utcNow, TrialLifecycleSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(options);
        if (tenant.TrialExpiresUtc is null || string.IsNullOrWhiteSpace(tenant.TrialStatus))
            return null;
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal))
            return null;
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
            return null;
        DateTimeOffset anchor = tenant.TrialExpiresUtc.Value;
        DateTimeOffset readOnlyNotBefore = anchor.AddDays(options.ReadOnlyAfterExpireDays);
        DateTimeOffset exportOnlyNotBefore = readOnlyNotBefore.AddDays(options.ExportOnlyAfterReadOnlyDays);
        DateTimeOffset purgeNotBefore = exportOnlyNotBefore.AddDays(options.PurgeAfterExportOnlyDays);
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
        {
            if (utcNow < anchor)
                return null;
            return new TrialLifecycleAdvancement(TrialLifecycleStatus.Active, TrialLifecycleStatus.Expired, "trial_active_window_ended");
        }

        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Expired, StringComparison.Ordinal))
        {
            if (utcNow < readOnlyNotBefore)
                return null;
            return new TrialLifecycleAdvancement(TrialLifecycleStatus.Expired, TrialLifecycleStatus.ReadOnly, "trial_read_only_phase");
        }

        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.ReadOnly, StringComparison.Ordinal))
        {
            if (utcNow < exportOnlyNotBefore)
                return null;
            return new TrialLifecycleAdvancement(TrialLifecycleStatus.ReadOnly, TrialLifecycleStatus.ExportOnly, "trial_export_only_phase");
        }

        if (!string.Equals(tenant.TrialStatus, TrialLifecycleStatus.ExportOnly, StringComparison.Ordinal))
            return null;
        if (utcNow < purgeNotBefore)
            return null;
        return new TrialLifecycleAdvancement(TrialLifecycleStatus.ExportOnly, TrialLifecycleStatus.Deleted, "trial_dpa_hard_purge");
    }

    /// <summary>Whole days until the next lifecycle boundary for <c>GET /v1/tenant/trial-status</c>.</summary>
    public static int? ComputeDaysRemainingForStatusDisplay(TenantRecord tenant, DateTimeOffset utcNow, TrialLifecycleSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(options);
        if (tenant.TrialExpiresUtc is null || string.IsNullOrWhiteSpace(tenant.TrialStatus))
            return null;
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal) || string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
            return null;
        DateTimeOffset T = tenant.TrialExpiresUtc.Value;
        DateTimeOffset readOnlyNotBefore = T.AddDays(options.ReadOnlyAfterExpireDays);
        DateTimeOffset exportOnlyNotBefore = readOnlyNotBefore.AddDays(options.ExportOnlyAfterReadOnlyDays);
        DateTimeOffset purgeNotBefore = exportOnlyNotBefore.AddDays(options.PurgeAfterExportOnlyDays);
        DateTimeOffset deadline = tenant.TrialStatus switch
        {
            TrialLifecycleStatus.Active => T,
            TrialLifecycleStatus.Expired => readOnlyNotBefore,
            TrialLifecycleStatus.ReadOnly => exportOnlyNotBefore,
            TrialLifecycleStatus.ExportOnly => purgeNotBefore,
            _ => T
        };
        double totalDays = (deadline - utcNow).TotalDays;
        int days = (int)Math.Floor(totalDays);
        return days < 0 ? 0 : days;
    }
}
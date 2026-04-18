namespace ArchLucid.Core.Configuration;

/// <summary>Background trial expiry / DPA deletion scheduler (Worker).</summary>
public sealed class TrialLifecycleSchedulerOptions
{
    public const string SectionName = "Trial:Lifecycle";

    /// <summary>Polling interval for <see cref="ArchLucid.Host.Core.Hosted.TrialLifecycleSchedulerHostedService"/>.</summary>
    public int IntervalMinutes { get; init; } = 360;

    /// <summary>Days in <c>Expired</c> after <c>TrialExpiresUtc</c> before moving to <c>ReadOnly</c>.</summary>
    public int ReadOnlyAfterExpireDays { get; init; } = 7;

    /// <summary>Days in <c>ReadOnly</c> before <c>ExportOnly</c>.</summary>
    public int ExportOnlyAfterReadOnlyDays { get; init; } = 30;

    /// <summary>Days in <c>ExportOnly</c> before hard purge (tenant row removed).</summary>
    public int PurgeAfterExportOnlyDays { get; init; } = 60;

    /// <summary>Max rows per <c>DELETE TOP</c> batch inside <see cref="Tenancy.ITenantHardPurgeService"/>.</summary>
    public int HardPurgeMaxRowsPerStatement { get; init; } = 5000;
}

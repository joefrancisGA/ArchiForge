namespace ArchiForge.Decisioning.Advisory.Scheduling;

/// <summary>
/// Tenant-scoped CRON-style definition for recurring advisory scans: which authority project slug to read and when the next run is due.
/// </summary>
/// <remarks>
/// Persisted in <c>dbo.AdvisoryScanSchedules</c> (SQL) or in-memory for tests. API requests set scope ids from the caller; <see cref="NextRunUtc"/> is computed at create/update via <see cref="IScanScheduleCalculator"/>.
/// Enabled schedules with non-null <see cref="NextRunUtc"/> ≤ “now” are returned by schedule repositories’ due queries for the hosted poller.
/// </remarks>
public class AdvisoryScanSchedule
{
    /// <summary>Default value used for <see cref="RunProjectSlug"/> when none is supplied.</summary>
    public const string DefaultProjectSlug = "default";

    /// <summary>Primary key for the schedule row.</summary>
    public Guid ScheduleId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant dimension of the advisory scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace dimension of the advisory scope.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project dimension of the advisory scope (governance/comparison context).</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Authority store <c>Runs.ProjectId</c> slug (e.g. <c>default</c>), not the scope GUID.</summary>
    /// <remarks>Trimmed when running scans; empty becomes <c>default</c>.</remarks>
    public string RunProjectSlug { get; set; } = DefaultProjectSlug;

    /// <summary>Human-readable label for operators and audit.</summary>
    public string Name { get; set; } = "Daily Advisory Scan";

    /// <summary>
    /// Cadence expression interpreted by <see cref="IScanScheduleCalculator"/> (v1 supports a small preset set plus daily 07:00 UTC).
    /// </summary>
    public string CronExpression { get; set; } = "0 7 * * *";

    /// <summary>When <see langword="false"/>, the schedule is ignored by due polling.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Row creation timestamp (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC time of the last completed or attempted run (updated when the runner advances the schedule).</summary>
    public DateTime? LastRunUtc { get; set; }

    /// <summary>UTC time when the next poll should consider this schedule due; <see langword="null"/> excludes it from due lists.</summary>
    public DateTime? NextRunUtc { get; set; }
}

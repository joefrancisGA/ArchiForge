namespace ArchiForge.Decisioning.Advisory.Scheduling;

/// <summary>
/// One attempt of an advisory scan for a <see cref="AdvisoryScanSchedule"/>, including lifecycle status and a JSON summary payload.
/// </summary>
/// <remarks>
/// Persisted in <c>dbo.AdvisoryScanExecutions</c>. The runner sets <see cref="Status"/> to <c>Started</c>, then <c>Completed</c> or <c>Failed</c>, and fills <see cref="ResultJson"/> with scan metadata (run ids, digest id, alert counts) on success.
/// </remarks>
public class AdvisoryScanExecution
{
    /// <summary>Primary key for this execution attempt.</summary>
    public Guid ExecutionId { get; set; } = Guid.NewGuid();

    /// <summary>Parent schedule (<see cref="AdvisoryScanSchedule.ScheduleId"/>).</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Tenant dimension (copied from the schedule for querying without a join).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace dimension (copied from the schedule).</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project dimension (copied from the schedule).</summary>
    public Guid ProjectId { get; set; }

    /// <summary>When the runner created the row (UTC).</summary>
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When the runner finished (success, no-runs, or failure), if applicable.</summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>Lifecycle marker: e.g. <c>Started</c>, <c>Completed</c>, <c>Failed</c>.</summary>
    public string Status { get; set; } = "Started";

    /// <summary>JSON blob with outcome details (empty object until completion).</summary>
    public string ResultJson { get; set; } = "{}";

    /// <summary>Human-readable failure reason when <see cref="Status"/> is <c>Failed</c>.</summary>
    public string? ErrorMessage { get; set; }
}

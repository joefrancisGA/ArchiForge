namespace ArchLucid.Host.Core.Configuration;

/// <summary>Background probes for cross-table consistency (SQL only).</summary>
public sealed class DataConsistencyProbeOptions
{
    /// <summary>Configuration section (<c>DataConsistency</c>).</summary>
    public const string SectionName = "DataConsistency";

    /// <summary>When true, the API/worker periodically counts orphan coordinator rows and emits metrics (detection-only).</summary>
    public bool OrphanProbeEnabled
    {
        get;
        set;
    } = true;

    /// <summary>Interval between probe passes.</summary>
    public int OrphanProbeIntervalMinutes
    {
        get;
        set;
    } = 60;

    /// <summary>
    /// When greater than zero, after a probe pass that detected any orphans, runs the same SELECT statements as admin
    /// remediation dry-run and logs candidate keys at Information level (never DELETE). Clamped to [1, 500]. Default 0 (disabled).
    /// </summary>
    public int OrphanProbeRemediationDryRunLogMaxRows
    {
        get;
        set;
    }

    /// <summary>
    /// When true, automatically soft-delete orphaned graph edges and nodes found by the probe.
    /// </summary>
    public bool EnableAutoRemediation
    {
        get;
        set;
    } = false;
}

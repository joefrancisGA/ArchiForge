namespace ArchLucid.Contracts.Governance;

/// <summary>
///     Per-run dry-run result: the run's actual finding metrics, the per-threshold breach map under the
///     proposed thresholds, and the headline <see cref="WouldBlock" /> verdict.
/// </summary>
public sealed class PolicyPackDryRunRunItem
{
    /// <summary>The run id the caller passed in <c>evaluateAgainstRunIds</c>.</summary>
    public string RunId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>
    ///     <see langword="true" /> when the run id could not be loaded (not found, out of scope, or read
    ///     failure). When true, all metric / breach fields are zero / empty and the row is excluded from
    ///     the <c>WouldBlock</c> / <c>WouldAllow</c> tallies in <see cref="PolicyPackDryRunDeltaCounts" />.
    /// </summary>
    public bool RunMissing
    {
        get;
        init;
    }

    /// <summary>Run-level finding counts grouped case-insensitively by severity (descending by count).</summary>
    public IReadOnlyList<PolicyPackDryRunSeverityCount> FindingsBySeverity
    {
        get;
        init;
    } = [];

    /// <summary>
    ///     Per-threshold-key breach map. Keys mirror <see cref="PolicyPackDryRunSupportedThresholdKeys" />
    ///     (e.g. <c>maxCriticalFindings</c>). Each entry shows the proposed value, the run's actual value,
    ///     and whether the proposed threshold is breached.
    /// </summary>
    public IReadOnlyList<PolicyPackDryRunThresholdOutcome> ThresholdOutcomes
    {
        get;
        init;
    } = [];

    /// <summary>
    ///     <see langword="true" /> when at least one of <see cref="ThresholdOutcomes" /> reports
    ///     <see cref="PolicyPackDryRunThresholdOutcome.WouldBreach" /> = <see langword="true" />.
    /// </summary>
    public bool WouldBlock
    {
        get;
        init;
    }
}

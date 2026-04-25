using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Alerts.Composite;

namespace ArchLucid.Decisioning.Alerts.Simulation;

/// <summary>
///     Input to <see cref="IRuleSimulationService.SimulateAsync" /> describing rule kind, payload, and which runs to
///     replay.
/// </summary>
public class RuleSimulationRequest
{
    /// <summary><c>Simple</c> or <c>Composite</c> (case-insensitive in service).</summary>
    public string RuleKind
    {
        get;
        set;
    } = null!;

    /// <summary>Required when <see cref="RuleKind" /> is Simple.</summary>
    public AlertRule? SimpleRule
    {
        get;
        set;
    }

    /// <summary>Required when <see cref="RuleKind" /> is Composite.</summary>
    public CompositeAlertRule? CompositeRule
    {
        get;
        set;
    }

    /// <summary>When set, builds a single context for this run (plus optional comparison).</summary>
    public Guid? RunId
    {
        get;
        set;
    }

    /// <summary>Baseline run for comparison when building the plan for <see cref="RunId" />.</summary>
    public Guid? ComparedToRunId
    {
        get;
        set;
    }

    /// <summary>When <see cref="RunId" /> is null, number of recent runs to pull (clamped in context provider).</summary>
    public int RecentRunCount
    {
        get;
        set;
    } = 5;

    /// <summary>
    ///     When false and <see cref="RunId" /> is null, returns no contexts (reserved for future single-current-run
    ///     modes).
    /// </summary>
    public bool UseHistoricalWindow
    {
        get;
        set;
    } = true;

    /// <summary>Authority slug for listing runs (e.g. scheduled advisory scans).</summary>
    public string RunProjectSlug
    {
        get;
        set;
    } = AdvisoryScanSchedule.DefaultProjectSlug;
}

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Aggregated pilot / proof-of-ROI metrics for a tenant and UTC window (audit export uses [<see cref="FromUtc" />,
///     <see cref="ToUtc" />) half-open).
/// </summary>
public sealed class PilotValueReport
{
    public Guid TenantId
    {
        get;
        init;
    }

    /// <summary>Inclusive lower bound for run selection and audit tallies.</summary>
    public DateTime FromUtc
    {
        get;
        init;
    }

    /// <summary>Exclusive upper bound (matches <see cref="ArchLucid.Persistence.Audit.IAuditRepository.GetExportAsync" />).</summary>
    public DateTime ToUtc
    {
        get;
        init;
    }

    public int TotalRunsCommitted
    {
        get;
        init;
    }

    /// <summary>
    ///     When true, finding/agent/timing aggregates consider at most <see cref="RunDetailCap" /> committed runs
    ///     (earliest in window by <c>CreatedUtc</c>).
    /// </summary>
    public bool RunDetailsTruncated
    {
        get;
        init;
    }

    public int RunDetailCap
    {
        get;
        init;
    }

    public int TotalFindings
    {
        get;
        init;
    }

    public PilotValueReportSeverityBreakdown FindingsBySeverity
    {
        get;
        init;
    } = new();

    /// <summary>Durable <see cref="ArchLucid.Core.Audit.AuditEventTypes.RecommendationGenerated" /> rows in the window.</summary>
    public int TotalRecommendationsProduced
    {
        get;
        init;
    }

    /// <summary>Average wall time from run creation to golden manifest metadata timestamp; null when no committed samples.</summary>
    public double? AveragePipelineCompletionSeconds
    {
        get;
        init;
    }

    public int GovernanceApprovals
    {
        get;
        init;
    }

    public int GovernanceRejections
    {
        get;
        init;
    }

    public int PolicyPackAssignments
    {
        get;
        init;
    }

    /// <summary>Runs, comparisons, governance simulations, and conflict signals (see <see cref="PilotValueReportService" />).</summary>
    public int ComparisonOrDriftDetections
    {
        get;
        init;
    }

    public IReadOnlyList<string> UniqueAgentTypes
    {
        get;
        init;
    } = [];

    public IReadOnlyList<PilotValueReportRunTimelinePoint> CommittedRunsTimeline
    {
        get;
        init;
    } = [];

    /// <summary>
    ///     Point-in-time pending approvals (from
    ///     <see cref="ArchLucid.Application.Governance.IGovernanceDashboardService" />).
    /// </summary>
    public int GovernancePendingApprovalsNow
    {
        get;
        init;
    }

    /// <summary>True when audit export hit the row cap (10k); governance/recommendation tallies may be incomplete.</summary>
    public bool AuditExportTruncated
    {
        get;
        init;
    }
}

public sealed class PilotValueReportSeverityBreakdown
{
    public int Critical
    {
        get;
        set;
    }

    public int High
    {
        get;
        set;
    }

    public int Medium
    {
        get;
        set;
    }

    public int Low
    {
        get;
        set;
    }

    public int Info
    {
        get;
        set;
    }
}

public sealed class PilotValueReportRunTimelinePoint
{
    public string RunId
    {
        get;
        init;
    } = string.Empty;

    public DateTime CreatedUtc
    {
        get;
        init;
    }

    public DateTime? CommittedUtc
    {
        get;
        init;
    }

    public string SystemName
    {
        get;
        init;
    } = string.Empty;
}

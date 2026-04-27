namespace ArchLucid.Persistence.Pilots;

/// <summary>Scalar aggregates for <c>GET /v1/pilots/scorecard</c> (tenant-scoped, usually from a single Dapper row).</summary>
public sealed class PilotScorecardTenantMetrics
{
    public int TotalRunsCommitted
    {
        get;
        init;
    }

    public int TotalManifestsCreated
    {
        get;
        init;
    }

    public int TotalFindingsResolved
    {
        get;
        init;
    }

    public double? AverageTimeToManifestMinutes
    {
        get;
        init;
    }

    public int TotalAuditEventsGenerated
    {
        get;
        init;
    }

    public int TotalGovernanceApprovalsCompleted
    {
        get;
        init;
    }

    public DateTimeOffset? FirstCommitUtc
    {
        get;
        init;
    }
}

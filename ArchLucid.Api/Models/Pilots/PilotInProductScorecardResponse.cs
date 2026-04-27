namespace ArchLucid.Api.Models.Pilots;

public sealed class PilotInProductScorecardResponse
{
    public Guid TenantId
    {
        get;
        set;
    }

    public int TotalRunsCommitted
    {
        get;
        set;
    }

    public int TotalManifestsCreated
    {
        get;
        set;
    }

    public int TotalFindingsResolved
    {
        get;
        set;
    }

    public double? AverageTimeToManifestMinutes
    {
        get;
        set;
    }

    public int TotalAuditEventsGenerated
    {
        get;
        set;
    }

    public int TotalGovernanceApprovalsCompleted
    {
        get;
        set;
    }

    public DateTimeOffset? FirstCommitUtc
    {
        get;
        set;
    }

    public int? DaysSinceFirstCommit
    {
        get;
        set;
    }

    public PilotInProductBaselinesResponse? Baselines
    {
        get;
        set;
    }

    public PilotInProductRoiEstimateResponse? RoiEstimate
    {
        get;
        set;
    }
}

public sealed class PilotInProductBaselinesResponse
{
    public decimal? BaselineHoursPerReview
    {
        get;
        set;
    }

    public int? BaselineReviewsPerQuarter
    {
        get;
        set;
    }

    public decimal? BaselineArchitectHourlyCost
    {
        get;
        set;
    }

    public DateTimeOffset UpdatedUtc
    {
        get;
        set;
    }
}

public sealed class PilotInProductRoiEstimateResponse
{
    public decimal AnnualReviewCostStatusQuoUsd
    {
        get;
        set;
    }

    public decimal AnnualReviewSavingsFromReviewTimeLeverUsd
    {
        get;
        set;
    }

    public string ModelReference
    {
        get;
        set;
    } = "";

    public string Currency
    {
        get;
        set;
    } = "USD";
}

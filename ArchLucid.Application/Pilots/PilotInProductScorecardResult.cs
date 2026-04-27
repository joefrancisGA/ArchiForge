namespace ArchLucid.Application.Pilots;

public sealed class PilotInProductScorecardResult
{
    public required Guid TenantId
    {
        get;
        init;
    }

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

    public int? DaysSinceFirstCommit
    {
        get;
        init;
    }

    public PilotInProductBaselinesView? Baselines
    {
        get;
        init;
    }

    public PilotInProductRoiEstimate? RoiEstimate
    {
        get;
        init;
    }
}

public sealed class PilotInProductBaselinesView
{
    public decimal? BaselineHoursPerReview
    {
        get;
        init;
    }

    public int? BaselineReviewsPerQuarter
    {
        get;
        init;
    }

    public decimal? BaselineArchitectHourlyCost
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedUtc
    {
        get;
        init;
    }
}

public sealed class PilotInProductRoiEstimate
{
    public decimal AnnualReviewCostStatusQuoUsd
    {
        get;
        init;
    }

    public decimal AnnualReviewSavingsFromReviewTimeLeverUsd
    {
        get;
        init;
    }

    public string ModelReference
    {
        get;
        init;
    } = "docs/go-to-market/ROI_MODEL.md §2–3 (50% review-hour reduction)";

    public string Currency
    {
        get;
        init;
    } = "USD";
}
